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

namespace Ceebeetle
{
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

        public ChatWnd()
        {
            m_exit = false;
            m_showConnectedCallback = new ShowOnConnectedD(ShowOnConnected);
            m_showMessageCallback = new ShowMessageD(ShowMessage);
            m_p2p = new CCBP2PNetworker();
            m_p2p.AddListener(this);
            InitializeComponent();
            SetHostNameTo(tbUserId);
            Validate();
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
                System.Diagnostics.Debug.Write("Exception getting user or host name: " + ex.Message);
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
                System.Diagnostics.Debug.Write("Null ref exception in ChatWnd.OnConnected. " + nex.Message);
            }
            catch (Exception fex)
            {
                System.Diagnostics.Debug.Write("Fatal(?) exception in ChatWnd.OnConnected. " + fex.Message);
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
                System.Diagnostics.Debug.Write("Null ref exception in ChatWnd.OnConnected. " + nex.Message);
            }
            catch (Exception fex)
            {
                System.Diagnostics.Debug.Write("Fatal(?) exception in ChatWnd.OnConnected. " + fex.Message);
            }
        }
        void INetworkListener.OnDisconnected()
        {
        }
        #endregion
        private void AddChatText(string text)
        {
            try
            {
                Paragraph pAdd = new Paragraph();

                pAdd.Inlines.Add(new Run(text));
                chatContent.Document.Blocks.Add(pAdd);
            }
            catch (NullReferenceException nex)
            {
                System.Diagnostics.Debug.Write("Null ref exception in ChatWnd. " + nex.Message);
            }
            catch (Exception fex)
            {
                System.Diagnostics.Debug.Write("Exception in ChatWnd, adding chat text. " + fex.Message);
            }
        }
        private void AddChatLink(string link)
        {
            try
            {
                Paragraph pAdd = new Paragraph();

                //pAdd.Inlines.Add(new Hyperlink((text));
                chatContent.Document.Blocks.Add(pAdd);
            }
            catch (NullReferenceException nex)
            {
                System.Diagnostics.Debug.Write("Null ref exception in ChatWnd. " + nex.Message);
            }
            catch (Exception fex)
            {
                System.Diagnostics.Debug.Write("Exception in ChatWnd, adding chat text. " + fex.Message);
            }
        }
        private void ShowOnConnected()
        {
            AddChatText("Connected as " + m_p2p.UserId);
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
            System.Diagnostics.Debug.Write("Closed and Exiting Chat Window object.");
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

    }
}
