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

namespace Ceebeetle
{
    /// <summary>
    /// Interaction logic for P2PReceiveFileWnd.xaml
    /// </summary>
    public partial class P2PReceiveFileWnd : CCBChildWindow
    {
        private CCBFileReceived m_filedata;
        private string m_filename;
        private string m_sender;

        public string Path
        {
            get { return tbFilename.Text; }
        }

        private P2PReceiveFileWnd()
        {
        }
        public P2PReceiveFileWnd(CCBFileReceived filedata)
        {
            m_filedata = filedata;
            InitializeComponent();
            Initialize();
            InitMinSize();
            Validat();
        }
        private void Initialize()
        {
            string tmpText = lSender.Content.ToString();

            lSender.Content = string.Format(tmpText, m_filedata.Sender);
            lFilename.Content = m_filedata.Name;
        }
        private void Validat()
        {
            btnReceive.IsEnabled = (0 < tbFilename.Text.Length);
        }

        private void tbFilename_TextChanged(object sender, TextChangedEventArgs e)
        {
            Validat();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            BrowseForSave(tbFilename, true);
        }

        private void btnReceive_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
