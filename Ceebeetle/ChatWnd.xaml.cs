using System;
using System.Collections.Generic;
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
        private bool m_exit;
        private CCBP2PNetworker m_p2p;
        private delegate void ShowOnConnectedD();
        private delegate void ShowMessageD(string uid, string message);
        ShowOnConnectedD m_showConnectedCallback;
        ShowMessageD m_showMessageCallback;
        private CCBFileReceived.FileRecivedPromptD m_fileReceivedCB;

        public ChatWnd()
        {
            m_exit = false;
            m_showConnectedCallback = new ShowOnConnectedD(ShowOnConnected);
            m_fileReceivedCB = new CCBFileReceived.FileRecivedPromptD(PromptForFileReceived);
            m_showMessageCallback = new ShowMessageD(ShowMessage);
            m_p2p = new CCBP2PNetworker();
            m_p2p.AddListener(this);
            InitializeComponent();
            SetHostNameTo(tbUserId);
            InitMinSize();
            InitChatWindow();
            Validate();
            btnSend.IsEnabled = false;
            btnSendFile.IsEnabled = false;
        }

        private void SetHostNameTo(TextBox tb)
        {
            try
            {
                string uName = Environment.UserName;

                if ((null != uName) && (0 < uName.Length))
                    tb.Text = uName;
                else
                    tb.Text = Dns.GetHostName();
            }
            catch (Exception ex)
            {
                Log("Exception getting user or host name: " + ex.Message);
            }
        }
        private void Validate()
        {
            btnConnect.IsEnabled = (0 != tbUserId.Text.Length);
        }

        #region INetworkListener
        void INetworkListener.OnMessage(string uid, string message)
        {
            try
            {
                string[] args = new string[2] { uid, message };

                Application.Current.Dispatcher.Invoke(m_showMessageCallback, args);
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
                Application.Current.Dispatcher.Invoke(m_showConnectedCallback);
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
        void INetworkListener.OnDisconnected()
        {
        }
        void PromptForFileReceived(CCBFileReceived filedata)
        {
            P2PReceiveFileWnd prompt = new P2PReceiveFileWnd(filedata);

            if (true == prompt.ShowDialog())
            {
                Log("Ready to receive the file {0}.", filedata.Name);
            }
        }
        void INetworkListener.OnReceivingFile(string sender, string filename)
        {
            CCBFileReceived filedata = new CCBFileReceived(sender, m_p2p.UserId, filename);

            this.Dispatcher.BeginInvoke(m_fileReceivedCB, new object[1] { filedata });
        }
        #endregion
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
        private void ShowOnConnected()
        {
            AddChatText("Connected as " + m_p2p.UserId, true);
            btnSend.IsEnabled = true;
            btnSendFile.IsEnabled = true;
        }
        private void ShowMessage(string uid, string message)
        {
            AddChatText(string.Format("{0}:{1}", uid, message));
        }

        public void Exit()
        {
            m_exit = true;
            Close();
            m_p2p.Stop();
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
            m_p2p.Start(tbUserId.Text);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
            P2PStartSendFile sendFileWnd = new P2PStartSendFile(m_p2p.GetKnownUsers());

            if (true == sendFileWnd.ShowDialog())
            {
                //Send it.
            }
        }

    }
}
