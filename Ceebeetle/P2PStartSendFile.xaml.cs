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
        private P2PStartSendFile()
        {
        }
        public P2PStartSendFile(string[] users)
        {
            InitializeComponent();
            InitMinSize();
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
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
