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
        nwc_disconnect,
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
        private bool m_working;
        private string m_uid;
        private Thread m_worker;
        private ManualResetEvent m_closeSignal, m_filexferSignal;
        private AutoResetEvent m_cmdSignal;
        private ICeebeetlePeer m_clientChannel;
        private Queue<CCBNetworkerCommandData> m_commandList;
        private CeebeetlePeerImpl m_peer;
        private CCBP2PFileWorker m_fileWorker;
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
            m_working = true;
            m_commandList = new Queue<CCBNetworkerCommandData>();
            m_closeSignal = new ManualResetEvent(false);
            m_filexferSignal = new ManualResetEvent(false);
            m_cmdSignal = new AutoResetEvent(false);
            m_worker = new Thread(new ThreadStart(Listener));
            m_factory = null;
            m_clientChannel = null;
            m_fileWorker = null;
            m_peer = new CeebeetlePeerImpl();
            m_peer.PingCallback = new CeebeetlePeerImpl.OnPingedD(PingCallback);
            m_peer.FileTransferResponseCallback = new CeebeetlePeerImpl.OnFileTransferResponseD(OnFileTransferResponse);
        }

        public string[] GetKnownUsers(bool inclSelf = true)
        {
            return m_peer.GetKnownUsers(inclSelf);
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
            if (null != m_fileWorker)
            {
                if (null != m_peer)
                    m_peer.RemoveListener(m_fileWorker);
                m_fileWorker.Stop();
                m_fileWorker = null;
            }
            m_closeSignal.Set();
            m_worker.Join();
            if (null != m_host)
            {
                m_host.Close();
                m_host = null;
            }
        }
        private void Close()
        {
            m_working = false;
            try
            {
                if (null != m_clientChannel)
                    ((ICommunicationObject)m_clientChannel).Close();
            }
            catch (CommunicationException commEx)
            {
                System.Diagnostics.Debug.Write(string.Format("Communication when closing channel: {0}", commEx.Message));
                ((ICommunicationObject)m_clientChannel).Abort();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception when closing channel: {0}", ex.Message));
                ((ICommunicationObject)m_clientChannel).Abort();
            }
            finally
            {
                m_clientChannel = null;
            }
            try
            {
                if (null != m_factory)
                    m_factory.Close();
            }
            catch (CommunicationException commEx)
            {
                System.Diagnostics.Debug.Write(string.Format("Communication when closing factory: {0}", commEx.Message));
                m_factory.Abort();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(string.Format("Exception when closing factory: {0}", ex.Message));
                m_factory.Abort();
            }
            finally
            {
                m_factory = null;
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
        private CCBP2PFileWorker GetFileWorker()
        {
            CCBP2PFileWorker fileWorker = m_fileWorker;

            //Accessing fileworker from multiple threads, so use singleton pattern.
            if (null == fileWorker)
            {
                lock (this)
                {
                    if (null == m_fileWorker)
                    {
                        m_fileWorker = new CCBP2PFileWorker();
                        m_peer.AddListener(m_fileWorker);
                    }
                    fileWorker = m_fileWorker;
                }
            }
            return fileWorker;
        }
        private void OnFileTransferResponse(string recipient, string filename, bool accept)
        {
            if (accept)
            {
                CCBP2PFileWorker fileWorker = GetFileWorker();

                fileWorker.FileRequested(recipient, filename);
                m_filexferSignal.Set();
            }
            else
            {
                //If transfer was canceled and we haven't started the fileworker, no need to do so now.
                if (null != m_fileWorker)
                    m_fileWorker.FileCanceled(recipient, filename);
            }
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
        public void StartDisconnect()
        {
            QueueCommand(new CCBNetworkerCommandData(CCBNetworkerCommand.nwc_disconnect));
            m_cmdSignal.Set();
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
                    case CCBNetworkerCommand.nwc_disconnect:
                        m_working = false;
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
                            m_clientChannel.RequestFile(cmd.m_data[0], m_uid, cmd.m_data[1]);
                        break;
                    case CCBNetworkerCommand.nwc_cancelFileTransfer:
                        if (null != m_clientChannel)
                            m_clientChannel.CancelFile(cmd.m_data[0], m_uid, cmd.m_data[1]);
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
            WaitHandle[] waitors = new WaitHandle[3] { m_closeSignal, m_cmdSignal, m_filexferSignal };

            if (null == m_host)
            {
                m_host = new ServiceHost(this);
                m_host.Open();
            }
            while (m_working)
            {
                int ixSig = WaitHandle.WaitAny(waitors);

                if (0 == ixSig)
                    break;
                else if (1 == ixSig)
                {
                    ExecuteNextCommand();
                }
                else if ((2 == ixSig) && (null != m_clientChannel))
                {
                    CCBP2PFileWorker fileWorker = GetFileWorker();
                    string fileToSend = fileWorker.HasData();

                    if (null != fileToSend)
                    {
                        CCBP2PFileDataEnvelope dataTosend = null;
                        int cb = fileWorker.RetrieveDataToSend(fileToSend, ref dataTosend);

                        if ((0 != cb) && (null != dataTosend))
                        {
                            try
                            {
                                System.Diagnostics.Debug.Write(string.Format("Sending {0} bytes from {1}", cb, dataTosend.m_localFileName));
                                m_clientChannel.SendFileData(m_uid, dataTosend.m_recipient, dataTosend.m_localFileName, dataTosend.m_bytes);
                                fileWorker.MarkDataSent(fileToSend, dataTosend);
                            }
                            catch (CommunicationException commEx)
                            {
                                System.Diagnostics.Debug.Write("Exception sending file data: " + commEx.Message);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.Write("Exception sending file data: " + ex.Message);
                            }
                        }
                    }
                }
                else
                    System.Diagnostics.Debug.Write(string.Format("Error return waiting for handles in networker: {0}", ixSig));
            }
            Close();
            m_peer.OnDisconnected();
            System.Diagnostics.Debug.Write("Chat listener thread exiting.");
        }

    }


}
