using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
    /// Interaction logic for P2PStartSendFile.xaml
    /// </summary>
    public partial class P2PStartSendFile : CCBChildWindow
    {
        private string m_recipient;
        public string Recipient
        {
            get { return m_recipient; }
        }
        public string Filename
        {
            get { return tbFile.Text; }
        }

        private P2PStartSendFile()
        {
        }
        public P2PStartSendFile(string[] users)
        {
            InitializeComponent();
            CeebeetleWindowInit();
            Populate(users);
            Validat();
        }

        private void Populate(string[] users)
        {
            if (null != users) foreach (string user in users)
                    lbUsers.Items.Add(user);
        }
        public void Validat()
        {
            btnSend.IsEnabled = ((0 < tbFile.Text.Length) && 
                                    (File.Exists(tbFile.Text)) && 
                                    (-1 != lbUsers.SelectedIndex));
        }

        private void tbFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            Validat();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            BrowseForFile(tbFile);
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            Assert(-1 != lbUsers.SelectedIndex);
            Assert(0 < tbFile.Text.Length);
            if ((-1 != lbUsers.SelectedIndex) && (0 < tbFile.Text.Length))
            {
                m_recipient = lbUsers.Items[lbUsers.SelectedIndex].ToString();
                DialogResult = true;
            }
            else
                DialogResult = false;
        }

        private void lbUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Validat();
        }
    }
}
