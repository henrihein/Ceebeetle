using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using System.Timers;
using System.Threading;

namespace Ceebeetle
{
    public class CCBFileReceived
    {
        public delegate void FileRecivedPromptD(CCBFileReceived filedata);
        private string m_sender;
        private string m_uid;
        private string m_filename;

        public string Sender
        {
            get { return m_sender; }
        }
        public string Name
        {
            get { return m_filename; }
        }

        private CCBFileReceived()
        {
        }
        public CCBFileReceived(string sender, string uid, string filename)
        {
            m_sender = sender;
            m_filename = filename;
            m_uid = uid;
        }
    }

    /// <summary>
    /// Interaction logic for ChatWnd.xaml
    /// </summary>
    public partial class ChatWnd : CCBChildWindow, INetworkListener
    {
        private bool m_exit, m_connected, m_wasConnected;
        private CCBP2PNetworker m_p2p;
        private delegate void DShowOnConnected();
        private delegate void DShowMessage(string uid, string message);
        private delegate void DShowLastError();
        private delegate void DShowUserConnect(string uid);
        DShowOnConnected m_showConnectedCallback;
        DShowMessage m_showMessageCallback;
        DShowUserConnect m_showUserConnectCallback;
        DShowLastError m_showLastErrorCallback;
        private CCBFileReceived.FileRecivedPromptD m_fileReceivedCB;
        List<string> m_errorList;

        public bool IsDefunct
        {
            get { return m_wasConnected && !m_connected; }
        }

        public ChatWnd()
        {
            m_errorList = new List<string>();
            m_exit = false;
            m_connected = false;
            m_wasConnected = false;
            m_showConnectedCallback = new DShowOnConnected(ShowOnConnected);
            m_fileReceivedCB = new CCBFileReceived.FileRecivedPromptD(PromptForFileReceived);
            m_showUserConnectCallback = new DShowUserConnect(ShowUserConnect);
            m_showMessageCallback = new DShowMessage(ShowMessage);
            m_showLastErrorCallback = new DShowLastError(ShowLastError);
            m_p2p = new CCBP2PNetworker();
            m_p2p.AddListener(this);
            m_p2p.OnFileTransferErrorCallback = new DOnFileTransferError(OnFileTransferError);
            InitializeComponent();
            SetHostNameTo(tbUserId);
            InitMinSize();
            InitChatWindow();
            Validate();
            EnableUI(false);
        }

        private void SetHostNameTo(TextBox tb)
        {
            try
            {
                string uName = Dns.GetHostName() + "." + Environment.UserName;

                if ((null != uName) && (0 < uName.Length))
                    tb.Text = uName;
                else
                    tb.Text = string.Format("user{0}", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                Log("Exception getting user or host name: " + ex.Message);
                tb.Text = "rockstar";
            }
        }
        private void Validate()
        {
            if (m_connected)
            {
                btnConnect.Content = "_Disconnect";
                btnConnect.IsEnabled = true;
            }
            else
            {
                btnConnect.IsEnabled = true;
                btnConnect.IsEnabled = (0 != tbUserId.Text.Length);
            }
        }

        #region INetworkListener
        void INetworkListener.OnMessage(string uid, string message)
        {
            try
            {
                string[] args = new string[2] { uid, message };

                Application.Current.Dispatcher.BeginInvoke(m_showMessageCallback, args);
            }
            catch (NullReferenceException nex)
            {
                Log("Null ref exception in ChatWnd.OnConnected. " + nex.Message);
            }
            catch (Exception fex)
            {
                Log("Fatal(?) exception in ChatWnd.OnConnected. " + fex.Message);
            }
        }
        void INetworkListener.OnConnected()
        {
            try
            {
                m_connected = true;
                Application.Current.Dispatcher.BeginInvoke(m_showConnectedCallback);
            }
            catch (NullReferenceException nex)
            {
                Log("Null ref exception in ChatWnd.OnConnected. " + nex.Message);
            }
            catch (Exception fex)
            {
                Log("Fatal(?) exception in ChatWnd.OnConnected. " + fex.Message);
            }
        }
        void INetworkListener.OnUser(string uid)
        {
            Application.Current.Dispatcher.BeginInvoke(m_showUserConnectCallback, new object[1]{uid});
        }
        void INetworkListener.OnDisconnected()
        {
            try
            {
                if (m_connected)
                    m_wasConnected = true;
                m_connected = false;
                Application.Current.Dispatcher.BeginInvoke(m_showConnectedCallback);
            }
            catch (NullReferenceException nex)
            {
                Log("Null ref exception in ChatWnd.OnConnected. " + nex.Message);
            }
            catch (Exception fex)
            {
                Log("Fatal(?) exception in ChatWnd.OnConnected. " + fex.Message);
            }
        }
        void INetworkListener.OnFileData(string filename, long offset, byte[] data)
        {
        }
        void INetworkListener.OnFileComplete(string filename, byte[] hash)
        {
        }
        void PromptForFileReceived(CCBFileReceived filedata)
        {
            P2PReceiveFileWnd prompt = new P2PReceiveFileWnd(filedata);

            prompt.Owner = this;
            if (true == prompt.ShowDialog())
            {
                Log("Ready to receive the file {0} to {1}.", filedata.Name, prompt.Path);
                m_p2p.RequestFileTransfer(filedata.Sender, filedata.Name, prompt.Path);
            }
            else
                m_p2p.CancelFileTransfer(filedata.Sender, filedata.Name);
        }
        void INetworkListener.OnReceivingFile(string sender, string filename)
        {
            CCBFileReceived filedata = new CCBFileReceived(sender, m_p2p.UserId, filename);

            this.Dispatcher.BeginInvoke(m_fileReceivedCB, new object[1] { filedata });
        }
        void INetworkListener.OnFileError(string sender, string recipient, string filename)
        {
            if (m_p2p.IsMe(recipient))
                ShowError("Remote error on " + filename);
        }
        #endregion
        private void OnFileTransferError(string sender, string filename)
        {
            ShowError(string.Format("{0} from {1} had an error. Download canceled.", filename, sender));
        }
        private void InitChatWindow()
        {
            FlowDocument doc = chatContent.Document;

            Assert(null != doc);
            if (null != doc)
            {
                doc.FontFamily = new FontFamily("Lucida Console");
                doc.FontSize = 9;
                doc.PagePadding = new Thickness(0);
            }
        }
        private void AddChatText(string text, bool bold = false)
        {
            try
            {
                Paragraph pAdd = new Paragraph();

                if (bold)
                    pAdd.Inlines.Add(new Bold(new Run(text)));
                else
                    pAdd.Inlines.Add(new Run(text));
                pAdd.Padding = new Thickness(1);
                if (bold)
                    pAdd.Margin = new Thickness(1);
                else
                    pAdd.Margin = new Thickness(0);
                chatContent.Document.Blocks.Add(pAdd);
            }
            catch (NullReferenceException nex)
            {
                Log("Null ref exception in ChatWnd. " + nex.Message);
            }
            catch (Exception fex)
            {
                Log("Exception in ChatWnd, adding chat text. " + fex.Message);
            }
        }
        private void AddErrorText(string text)
        {
            try
            {
                Paragraph pAdd = new Paragraph();

                pAdd.Background = Brushes.AntiqueWhite;
                pAdd.Foreground = Brushes.DarkRed;
                pAdd.FontSize = chatContent.Document.FontSize - 2;
                pAdd.Inlines.Add(new Run(text));
                pAdd.Padding = new Thickness(1);
                pAdd.Margin = new Thickness(0);
                chatContent.Document.Blocks.Add(pAdd);
            }
            catch (Exception fex)
            {
                Log("Exception in ChatWnd, adding chat text. " + fex.Message);
            }
        }
        private void EnableUI(bool enable = true)
        {
            btnSend.IsEnabled = enable;
            btnSendFile.IsEnabled = enable;
        }
        private void ShowOnConnected()
        {
            if (m_connected)
            {
                AddChatText("Connected as " + m_p2p.UserId, true);
                Validate();
                EnableUI();
                //We need to know the other users. 
                //Send out a ping to the other clients; they will report back with OnUserConnected.
                m_p2p.PingMesh();
            }
            else
            {
                Hide();
                Exit();
                Close();
            }
        }
        private void ShowUserConnect(string uid)
        {
            AddChatText("User connected: " + uid);
        }
        private void ShowMessage(string uid, string message)
        {
            AddChatText(string.Format("{0}:{1}", uid, message));
        }
        private void ShowLastError()
        {
            lock (m_errorList)
            {
                if (0 < m_errorList.Count)
                    lStatus.Content = m_errorList[m_errorList.Count - 1];
            }
        }
        private void ShowError(string errorText)
        {
            CCBLogConfig.GetLogger().Error("Adding Chat window error: {0}", errorText);
            lock (m_errorList)
            {
                m_errorList.Add(errorText);
            }
            Application.Current.Dispatcher.BeginInvoke(m_showLastErrorCallback);
        }
        public void Exit()
        {
            m_exit = true;
            m_p2p.RemoveListener(this);
            m_p2p.Stop();
            Close();
            Log("Closed and Exiting Chat Window object.");
        }

        private void CCBChildWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!m_exit)
            {
                Hide();
                e.Cancel = true;
            }
        }
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            btnConnect.IsEnabled = false;
            EnableUI(false);
            if (m_connected)
            {
                m_p2p.StartDisconnect();
            }
            else
                m_p2p.Start(tbUserId.Text);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void btnErrorList_Click(object sender, RoutedEventArgs e)
        {
            lock (m_errorList)
            {
                foreach (string errText in m_errorList)
                    AddErrorText(errText);
            }
        }
        private void btnConnect_TextInput(object sender, TextCompositionEventArgs e)
        {
            Validate();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            m_p2p.PostMessage(tbChatText.Text);
        }
        private void btnSendFile_Click(object sender, RoutedEventArgs e)
        {
            P2PStartSendFile sendFileWnd = new P2PStartSendFile(m_p2p.GetKnownUsers(false));

            sendFileWnd.Owner = this;
            if (true == sendFileWnd.ShowDialog())
            {
                //Check it and Send it.
                if (File.Exists(sendFileWnd.Filename))
                {
                    if (!m_p2p.StartFileTransfer(sendFileWnd.Recipient, sendFileWnd.Filename))
                        ShowError("Already sending " + sendFileWnd.Filename);
                }
                else
                    ShowError(string.Format("{0} does not exist, canceling send.", sendFileWnd.Filename));
            }
        }

    }
}
