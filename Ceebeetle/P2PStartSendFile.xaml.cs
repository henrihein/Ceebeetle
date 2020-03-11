using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Timers;
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
        public delegate void DOnProgressUpdate(List<CCBFileProgress.CCBFileProgressData> fpDataList);
        Timer m_timer;
        private DGetFileStatus m_fileStatusCallback;
        private DOnProgressUpdate m_onProgressUpdateCallback;
        List<CCBFileProgress> m_progressList;
        private string m_recipient;
        public string Recipient
        {
            get { return m_recipient; }
        }
        public string Filename
        {
            get { return tbFile.Text; }
        }

        public P2PStartSendFile(string[] users, string[] filelist, DGetFileStatus fileStatusCallback)
        {
            m_fileStatusCallback = fileStatusCallback;
            m_progressList = new List<CCBFileProgress>();
            m_timer = new Timer(1301);
            m_timer.Elapsed += new ElapsedEventHandler(OnTimer);
            m_timer.Start();
            m_onProgressUpdateCallback = new DOnProgressUpdate(OnProgressUpdate);
            InitializeComponent();
            CeebeetleWindowInit();
            Populate(users, filelist);
            Validat();
        }

        private void Populate(string[] users, string[] filelist)
        {
            if (null != users) foreach (string user in users)
                    lbUsers.Items.Add(user);
            if (null != filelist)
                foreach (string pathname in filelist)
                {
                    m_progressList.Add(CCBFileProgress.NewInstance(spStatus, pathname));
                }
        }
        private void AddTestItem(string itemText, int cb)
        {
            Label lItem = new Label();
            ProgressBar pb = new ProgressBar();

            lItem.Content = itemText;
            pb.Value = cb;
            spStatus.Children.Add(lItem);
            spStatus.Children.Add(pb);
        }
        private void OnProgressUpdate(List<CCBFileProgress.CCBFileProgressData> fpDataList)
        {
            foreach(CCBFileProgress.CCBFileProgressData fpData in fpDataList)
                fpData.OnProgressUpdate();
        }
        private void OnTimer(object source, ElapsedEventArgs evtArgs)
        {
            if (null != m_fileStatusCallback)
            {
                List<CCBFileProgress.CCBFileProgressData> needsUpdate = new List<CCBFileProgress.CCBFileProgressData>();

                lock(m_progressList)
                {
                    long cbCur, cbMax;

                    foreach(CCBFileProgress fp in m_progressList)
                    {
                        if (TStatusUpdate.tsuFileWork == m_fileStatusCallback(fp.Filename, out cbCur, out cbMax))
                        {
                            if (!fp.IsCurrent(cbCur, cbMax))
                                needsUpdate.Add(new CCBFileProgress.CCBFileProgressData(fp, cbCur, cbMax));
                        }
                    }
                }
                Application.Current.Dispatcher.Invoke(new DOnProgressUpdate(OnProgressUpdate), new object[1] { needsUpdate });
#if false
                foreach (CCBFileProgress.CCBFileProgressData fpData in needsUpdate)
                {
                    Application.Current.Dispatcher.Invoke(new DOnProgressUpdate(OnProgressUpdate), new object[1] {fpData});
                    //fpData.OnProgressUpdate();
                }
#endif
            }
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

        private void CCBChildWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_timer.Stop();
            m_timer.Dispose();
        }
    }
}
