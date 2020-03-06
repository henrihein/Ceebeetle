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
    class CCBP2PNetworker : CCBLogging
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
        private DOnFileTransferDone m_onFileDone;
        private DSelectStoreToPublish m_selectStoreCallback;

        #region WCFObjects
        private ServiceHost m_host;
        private DuplexChannelFactory<ICeebeetlePeer> m_factory;
        private InstanceContext m_site;
        #endregion

        public string UserId
        {
            get { return m_uid; }
        }
        public DOnFileTransferDone OnFileTransferDoneCallback
        {
            set { m_onFileDone = value; }
        }
        public DSelectStoreToPublish SelectStoreCallback
        {
            set { m_selectStoreCallback = value; }
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
            m_onFileDone = null;
            m_selectStoreCallback = null;
        }

        public string[] GetKnownUsers(bool inclSelf = true)
        {
            return m_peer.GetKnownUsers(inclSelf);
        }
        public bool IsMe(string uid)
        {
            if (null != m_uid)
                return 0 == string.Compare(m_uid, uid);
            return false;
        }
        public void AddListener(INetworkListener listener)
        {
            m_peer.AddListener(listener);
        }
        public void RemoveListener(INetworkListener listener)
        {
            m_peer.RemoveListener(listener);
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
            m_closeSignal.Set();
            if (null != m_fileWorker)
            {
                if (null != m_peer)
                    m_peer.RemoveListener(m_fileWorker);
                m_fileWorker.Stop();
                m_fileWorker = null;
            }
            if (m_worker.IsAlive)
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
                Error(string.Format("Communication when closing channel: {0}", commEx.Message));
                ((ICommunicationObject)m_clientChannel).Abort();
            }
            catch (Exception ex)
            {
                Error(string.Format("Exception when closing channel: {0}", ex.Message));
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
                Error(string.Format("Communication when closing factory: {0}", commEx.Message));
                m_factory.Abort();
            }
            catch (Exception ex)
            {
                Error(string.Format("Exception when closing factory: {0}", ex.Message));
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
        public bool StartFileTransfer(string recipient, string filename)
        {
            if (null != m_fileWorker)
            {
                if (m_fileWorker.IsUploading(filename))
                    return false;
            }
            QueueCommand(new CCBNetworkerCommandData(CCBNetworkerCommand.nwc_startFileTransfer, recipient, filename));
            return true;
        }
        public void RequestFileTransfer(string sender, string remoteFilename, string localFilename)
        {
            CCBP2PFileWorker fileWorker = GetFileWorker();

            fileWorker.PrepareInFile(sender, m_uid, remoteFilename, localFilename);
            QueueCommand(new CCBNetworkerCommandData(CCBNetworkerCommand.nwc_requestFileTransfer, sender, remoteFilename));
            m_filexferSignal.Set();
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
                        m_fileWorker = new CCBP2PFileWorker(m_closeSignal);
                        m_peer.AddListener(m_fileWorker);
                    }
                    fileWorker = m_fileWorker;
                    fileWorker.OnFileTransferDoneCallback = m_onFileDone;
                }
            }
            return fileWorker;
        }
        private void OnFileTransferResponse(string recipient, string filename, bool accept)
        {
            if (accept)
            {
                CCBP2PFileWorker fileWorker = GetFileWorker();

                fileWorker.FileRequested(m_uid, recipient, filename);
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
                    if (0 < m_commandList.Count)
                    {
                        CCBNetworkerCommandData cmd = m_commandList.Dequeue();

                        return cmd;
                    }
                }
            }
            catch (InvalidOperationException eex)
            {
                Error("Internal error: trying to get command while command list is empty." + eex.Message);
            }
            catch (Exception ex)
            {
                Error(string.Format("Error getting command: {0}", ex.Message));
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
                Error(string.Format("Communication exception connecting: {0}", commEx.Message));
            }
            catch (Exception ex)
            {
                Error(string.Format("Exception connecting: {0}", ex.Message));
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
                                Error("Internal error: m_data does not contain data.");
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
                        Error(string.Format("Networker: Ignoring {0} command.", cmd.m_cmd));
                        break;
                }
            }
            catch (NullReferenceException nex)
            {
                Error("Null reference in ExecuteNextCommand: " + nex.Message);
            }
            catch (Exception ex)
            {
                Error("Exception in ExecuteNextCommand: " + ex.Message);
            }
        }
        private bool CheckFileWorkerForErrors(CCBP2PFileWorker fileworker)
        {
            string sender = null;
            string recipient = null;
            string filename = fileworker.HasErrorFile(ref sender, ref recipient);

            if (null != filename)
            {
                m_clientChannel.OnFileError(sender, recipient, filename);
                return true;
            }
            return false;
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
                    string fileToSend = fileWorker.HasUploadWork();

                    if (null != fileToSend)
                    {
                        CCBP2PFileDataEnvelope dataTosend = null;
                        int cb = fileWorker.RetrieveDataToSend(fileToSend, ref dataTosend);
                        byte[] hash = null;
                        string recipient = null;

                        if ((0 != cb) && (null != dataTosend))
                        {
                            try
                            {
                                System.Diagnostics.Debug.Write(string.Format("Sending {0} bytes from {1}", cb, dataTosend.m_localFileName));
                                m_clientChannel.SendFileData(m_uid, dataTosend.m_recipient, dataTosend.m_localFileName, dataTosend.m_start, dataTosend.m_bytes);
                                fileWorker.MarkDataSent(fileToSend, dataTosend);
                                if (fileWorker.IsSent(fileToSend, ref hash, ref recipient))
                                {
                                    m_clientChannel.OnFileComplete(m_uid, recipient, dataTosend.m_localFileName, hash);
                                    fileWorker.FileFinalized(fileToSend);
                                }
                            }
                            catch (CommunicationException commEx)
                            {
                                Error("Exception sending file data: " + commEx.Message);
                                fileWorker.FileOnError(fileToSend);
                            }
                            catch (Exception ex)
                            {
                                Error("Exception sending file data: " + ex.Message);
                                fileWorker.FileOnError(fileToSend);
                            }
                        }
                        else if (fileWorker.IsSent(fileToSend, ref hash, ref recipient))
                        {
                            m_clientChannel.OnFileComplete(m_uid, recipient, fileToSend, hash);
                            fileWorker.FileFinalized(fileToSend);
                        }
                    }
                    if (fileWorker.HasWork())
                    {
                        if (m_closeSignal.WaitOne(73))
                            break;
                        while (CheckFileWorkerForErrors(fileWorker))
                        {
                            if (m_closeSignal.WaitOne(0))
                                break;
                        }
                    }
                    else
                        m_filexferSignal.Reset();
                }
                else
                    Fatal(string.Format("Error return waiting for handles in networker: {0}", ixSig));
            }
            Close();
            m_peer.OnDisconnected();
            Debug("Chat listener thread exiting.");
        }

    }
}
