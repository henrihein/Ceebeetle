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

        private P2PReceiveFileWnd()
        {
        }
        public P2PReceiveFileWnd(CCBFileReceived filedata)
        {
            m_filedata = filedata;
            InitializeComponent();
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
            BrowseForFile(tbFilename);
        }
    }
}
