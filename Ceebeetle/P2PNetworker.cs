using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ServiceModel;

namespace Ceebeetle
{
    public enum CCBNetworkerCommand
    {
        nwc_none = 0,
        nwc_connect,
        nwc_post,
        nwc_pingMesh,
        nwc_pingRespond,
        nwc_startFileTransfer,
        nwc_requestFileTransfer,
        nwc_cancelFileTransfer
    }

    public struct CCBNetworkerCommandData
    {
        public CCBNetworkerCommand m_cmd;
        public string[] m_data;
        public CCBNetworkerCommandData(CCBNetworkerCommand cmd, string data)
        {
            m_cmd = cmd;
            m_data = new string[1]{ data };
        }
        public CCBNetworkerCommandData(CCBNetworkerCommand cmd, string data1, string data2)
        {
            m_cmd = cmd;
            m_data = new string[2] { data1, data2 };
        }
        public CCBNetworkerCommandData(CCBNetworkerCommand cmd)
        {
            m_cmd = cmd;
            m_data = null;
        }
        public CCBNetworkerCommandData(CCBNetworkerCommandData rhs)
        {
            m_cmd = rhs.m_cmd;
            m_data = rhs.m_data;
        }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class CCBP2PNetworker
    {
        private string m_uid;
        private Thread m_worker;
        private ManualResetEvent m_closeSignal;
        private AutoResetEvent m_cmdSignal;
        private ICeebeetlePeer m_clientChannel;
        private Queue<CCBNetworkerCommandData> m_commandList;
        private CeebeetlePeerImpl m_peer;
        #region WCFObjects
        private ServiceHost m_host;
        private DuplexChannelFactory<ICeebeetlePeer> m_factory;
        private InstanceContext m_site;
        #endregion

        public string UserId
        {
            get { return m_uid; }
        }

        public CCBP2PNetworker()
        {
            m_commandList = new Queue<CCBNetworkerCommandData>();
            m_closeSignal = new ManualResetEvent(false);
            m_cmdSignal = new AutoResetEvent(false);
            m_worker = new Thread(new ThreadStart(Listener));
            m_factory = null;
            m_clientChannel = null;
            m_peer = new CeebeetlePeerImpl();
            m_peer.PingCallback = new CeebeetlePeerImpl.OnPingedD(PingCallback);
        }

        public string[] GetKnownUsers()
        {
            return m_peer.GetKnownUsers();
        }

        public void AddListener(INetworkListener listener)
        {
            m_peer.AddListener(listener);
        }
        public void RemoveListener(INetworkListener listener)
        {
        }
        public void Start(string uid)
        {
            m_uid = uid;
            m_peer.UserID = uid;
            m_closeSignal.Reset();
            m_cmdSignal.Reset();
            m_worker.Start();
            PostConnectCommand();
        }
        public void Stop()
        {
            if (m_worker.IsAlive)
            {
                m_closeSignal.Set();
                m_worker.Join();
            }
            if (null != m_host)
            {
                m_host.Close();
                m_host = null;
            }
        }
        public void PostMessage(string message)
        {
            QueueCommand(new CCBNetworkerCommandData(CCBNetworkerCommand.nwc_post, message));
        }
        public void StartFileTransfer(string recipient, string filename)
        {
            QueueCommand(new CCBNetworkerCommandData(CCBNetworkerCommand.nwc_startFileTransfer, recipient, filename));
        }
        public void RequestFileTransfer(string sender, string filename)
        {
            QueueCommand(new CCBNetworkerCommandData(CCBNetworkerCommand.nwc_requestFileTransfer, sender, filename));
        }
        public void CancelFileTransfer(string sender, string filename)
        {
            QueueCommand(new CCBNetworkerCommandData(CCBNetworkerCommand.nwc_cancelFileTransfer, sender, filename));
        }
        private void QueueCommand(CCBNetworkerCommandData cmd)
        {
            lock (m_commandList)
            {
                m_commandList.Enqueue(cmd);
                m_cmdSignal.Set();
            }
        }
        private CCBNetworkerCommandData GetCommand()
        {
            try
            {
                lock (m_commandList)
                {
                    CCBNetworkerCommandData cmd = m_commandList.Dequeue();

                    return cmd;
                }
            }
            catch (InvalidOperationException eex)
            {
                System.Diagnostics.Debug.Write("Internal error: trying to get command while command list is empty." + eex.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(string.Format("Error getting command: {0}", ex.Message));
            }
            return new CCBNetworkerCommandData(CCBNetworkerCommand.nwc_none);
        }

        private bool Connect()
        {
            try
            {
                if (null != m_clientChannel)
                    ((ICommunicationObject)m_clientChannel).Close();
                if (null == m_factory)
                {
                    EndpointAddress address = new EndpointAddress("net.p2p://ceebeetleclient");
                    NetPeerTcpBinding binding = new NetPeerTcpBinding();

                    binding.Security.Mode = SecurityMode.None;
                    m_site = new InstanceContext(m_peer);
                    m_factory = new DuplexChannelFactory<ICeebeetlePeer>(m_site, binding, address);
                }
                m_clientChannel = m_factory.CreateChannel();
                ((ICommunicationObject)m_clientChannel).Open();
                m_peer.OnConnected();
                return true;
            }
            catch (CommunicationException commEx)
            {
                System.Diagnostics.Debug.Write(string.Format("Communication exception connecting: {0}", commEx.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception connecting: {0}", ex.Message));
            }
            return false;
        }
        public void PingMesh()
        {
            QueueCommand(new CCBNetworkerCommandData(CCBNetworkerCommand.nwc_pingMesh));
            m_cmdSignal.Set();
        }
        public void PingCallback()
        {
            QueueCommand(new CCBNetworkerCommandData(CCBNetworkerCommand.nwc_pingRespond));
            m_cmdSignal.Set();
        }

        private void PostConnectCommand()
        {
            QueueCommand(new CCBNetworkerCommandData(CCBNetworkerCommand.nwc_connect));
            m_cmdSignal.Set();
        }
        private void ExecuteNextCommand()
        {
            try
            {
                CCBNetworkerCommandData cmd = GetCommand();

                switch (cmd.m_cmd)
                {
                    case CCBNetworkerCommand.nwc_connect:
                        Connect();
                        break;
                    case CCBNetworkerCommand.nwc_post:
                        if (null != m_clientChannel)
                        {
                            System.Diagnostics.Debug.Assert((null != cmd.m_data) && (0 < cmd.m_data.Length));
                            if ((null != cmd.m_data) && (0 < cmd.m_data.Length))
                                m_clientChannel.ChatMessage(m_uid, cmd.m_data[0]);
                            else
                                System.Diagnostics.Debug.Write("Internal error: m_data does not contain data.");
                        }
                        break;
                    case CCBNetworkerCommand.nwc_pingMesh:
                        if (null != m_clientChannel)
                            m_clientChannel.PingAll(m_uid);
                        break;
                    case CCBNetworkerCommand.nwc_pingRespond:
                        if (null != m_clientChannel)
                            m_clientChannel.OnUserConnected(m_uid);
                        break;
                    case CCBNetworkerCommand.nwc_startFileTransfer:
                        if (null != m_clientChannel)
                            m_clientChannel.OnNewFile(m_uid, cmd.m_data[0], cmd.m_data[1]);
                        break;
                    case CCBNetworkerCommand.nwc_requestFileTransfer:
                        if (null != m_clientChannel)
                            m_clientChannel.RequestFile(m_uid, cmd.m_data[0], cmd.m_data[1]);
                        break;
                    case CCBNetworkerCommand.nwc_cancelFileTransfer:
                        if (null != m_clientChannel)
                            m_clientChannel.CancelFile(m_uid, cmd.m_data[0], cmd.m_data[1]);
                        break;
                    default:
                        System.Diagnostics.Debug.Write(string.Format("Networker: Ignoring {0} command.", cmd.m_cmd));
                        break;
                }
            }
            catch (NullReferenceException nex)
            {
                System.Diagnostics.Debug.Write("Null reference in ExecuteNextCommand: " + nex.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write("Exception in ExecuteNextCommand: " + ex.Message);
            }
        }
        public void Listener()
        {
            WaitHandle[] waitors = new WaitHandle[2] { m_closeSignal, m_cmdSignal };

            if (null == m_host)
            {
                m_host = new ServiceHost(this);
                m_host.Open();
            }
            for (; ; )
            {
                int ixSig = WaitHandle.WaitAny(waitors);

                if (0 == ixSig)
                    break;
                else if (1 == ixSig)
                {
                    ExecuteNextCommand();
                }
                else
                    System.Diagnostics.Debug.Write(string.Format("Error return waiting for handles in networker: {0}", ixSig));
            }
            System.Diagnostics.Debug.Write("Chat listener thread exiting.");
        }

    }
}
