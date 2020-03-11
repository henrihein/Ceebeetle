using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace Ceebeetle
{
    public class ControlHelpers
    {
        static public BitmapImage NewImage(string uri)
        {
            BitmapImage bmSrc = new BitmapImage();
            bmSrc.BeginInit();
            bmSrc.UriSource = new Uri(uri);
            bmSrc.EndInit();
            return bmSrc;
        }
    }

    public class StoreItemViewer
    {
        private CCBStoreItem m_item;
        public CCBStoreItem Item
        {
            get { return m_item; }
        }

        private StoreItemViewer()
        {
        }
        public StoreItemViewer(CCBStoreItem storeItem)
        {
            m_item = storeItem;
        }
        public StoreItemViewer(CCBBagItem bagItem)
        {
            m_item = (CCBStoreItem)bagItem;
        }

        public override string ToString()
        {
            if (null == m_item)
                return "Unknown";
            if (-1 == m_item.Count)
                return string.Format("{0} (Cost:{1})", m_item.Item, m_item.Cost);
            return string.Format("{0} (Cost:{1}, Limit:{2})", m_item.Item, m_item.Cost, m_item.Count);
        }
    }

    public class CCBProgressPanel : StackPanel
    {
        CCBFileProgress m_fileProgress;

        public CCBFileProgress FileProgress
        {
            get { return m_fileProgress; }
        }

        public CCBProgressPanel(CCBFileProgress fileProgress)
            : base()
        {
            m_fileProgress = fileProgress;
        }
    }
    public class CCBFileProgress
    {
        private string m_pathname;
        private long m_cbMax, m_cbCur;
        private ProgressBar m_progressBar;
        private Label m_label;
        private bool m_done;

        public struct CCBFileProgressData
        {
            CCBFileProgress m_fp;
            long m_cbCur;
            long m_cbMax;

            public CCBFileProgressData(CCBFileProgress fp, long cbCur, long cbMax)
            {
                m_fp = fp;
                m_cbCur = cbCur;
                m_cbMax = cbMax;
            }
            public void OnProgressUpdate()
            {
                m_fp.OnProgressUpdate(m_cbCur, m_cbMax);
            }
        }
        public string Filename
        {
            get { return m_pathname; }
        }
        static public CCBFileProgress Find(StackPanel spCtl, string filename)
        {
            foreach (UIElement ctl in spCtl.Children)
            {
                if (ctl is CCBProgressPanel)
                {
                    CCBProgressPanel spContent = (CCBProgressPanel)ctl;

                    if (spContent.FileProgress.m_pathname.Equals(filename))
                        return spContent.FileProgress;
                }
            }
            return null;
        }
        static public CCBFileProgress NewInstance(StackPanel parent, string pathname)
        {
            Label lblCtl = new Label();
            ProgressBar pbCtl = new ProgressBar();
            CCBFileProgress fp = new CCBFileProgress(pathname, pbCtl, lblCtl);
            CCBProgressPanel panelCtl = new CCBProgressPanel(fp);

            panelCtl.Children.Add(lblCtl);
            panelCtl.Children.Add(pbCtl);
            parent.Children.Add(panelCtl);
            return fp;
        }
        private CCBFileProgress(string pathname, ProgressBar pbCtl, Label lblCtl)
        {
            string filename = Path.GetFileName(pathname);

            m_cbMax = 0;
            m_cbCur = 0;
            m_done = false;
            m_pathname = pathname;
            m_progressBar = pbCtl;
            m_label = lblCtl;
            lblCtl.Content = filename;
        }
        public void OnProgressUpdate(long cbCur, long cbMax)
        {
            m_cbCur = cbCur;
            if (-1 != cbMax)
            {
                if (m_cbMax != cbMax)
                {
                    m_cbMax = cbMax;
                    m_progressBar.Maximum = cbMax;
                }
                if ((m_cbCur == m_cbMax) && !m_done)
                {
                    m_done = true;
                    m_label.Content = m_label.Content + " (Done)";
                }
            }
            m_progressBar.Value = cbCur;
        }
        public bool IsCurrent(long cbCur, long cbMax)
        {
            return (m_cbMax == cbMax) && (m_cbCur == cbCur);
        }
    }
}
