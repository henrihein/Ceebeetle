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
        nwc_post
    }

    public struct CCBNetworkerCommandData
    {
        public CCBNetworkerCommand m_cmd;
        public string m_data;
        public CCBNetworkerCommandData(CCBNetworkerCommand cmd, string data)
        {
            m_cmd = cmd;
            m_data = data;
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

    public interface INetworkListener
    {
        void OnMessage(string uid, string message);
        void OnConnected();
        void OnDisconnected();
    }

    [ServiceContract(CallbackContract = typeof(ICeebeetlePeer))]
    public interface ICeebeetlePeer
    {
        [OperationContract(IsOneWay = true)]
        void ChatMessage(string uid, string hello);
    }

    public class CeebeetlePeerImpl : ICeebeetlePeer
    {
        private HashSet<INetworkListener> m_listeners;

        public CeebeetlePeerImpl()
        {
            m_listeners = new HashSet<INetworkListener>();
        }

        public void AddListener(INetworkListener listener)
        {
            lock (m_listeners)
            {
                m_listeners.Add(listener);
            }
        }

        private INetworkListener[] GetListeners()
        {
            INetworkListener[] listeners = null;

            lock (m_listeners)
            {
                listeners = m_listeners.ToArray();
            }
            return listeners;
        }
        void ICeebeetlePeer.ChatMessage(string uid, string message)
        {
            try
            {
                INetworkListener[] listeners = GetListeners();

                foreach (INetworkListener listener in listeners)
                    listener.OnMessage(uid, message);
            }
            catch (System.IO.IOException ioex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in ChatMessage: {0}", ioex.Message));
            }
            catch (System.ServiceModel.CommunicationException commEx)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in ChatMessage: {0}", commEx.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in ChatMessage: {0}", ex.Message));
            }
        }
        public void OnConnected()
        {
            try
            {
                INetworkListener[] listeners = GetListeners();

                foreach (INetworkListener listener in listeners)
                    listener.OnConnected();
            }
            catch (System.IO.IOException ioex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in OnConnected: {0}", ioex.Message));
            }
            catch (System.ServiceModel.CommunicationException commEx)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in OnConnected: {0}", commEx.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception in OnConnected: {0}", ex.Message));
            }
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

        private void PostConnectCommand()
        {
            QueueCommand(new CCBNetworkerCommandData(CCBNetworkerCommand.nwc_connect));
            m_cmdSignal.Set();
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
                    CCBNetworkerCommandData cmd = GetCommand();

                    switch (cmd.m_cmd)
                    {
                        case CCBNetworkerCommand.nwc_connect:
                            Connect();
                            break;
                        case CCBNetworkerCommand.nwc_post:
                            System.Diagnostics.Debug.Write("Ready to post something....");
                            if (null != m_clientChannel)
                                m_clientChannel.ChatMessage(m_uid, cmd.m_data);
                            break;
                        default:
                            System.Diagnostics.Debug.Write(string.Format("Networker: Ignoring {0} command.", cmd.m_cmd));
                            break;
                    }
                }
                else
                    System.Diagnostics.Debug.Write(string.Format("Error return waiting for handles in networker: {0}", ixSig));
            }
            System.Diagnostics.Debug.Write("Chat listener thread exiting.");
        }

    }
}
