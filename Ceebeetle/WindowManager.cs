using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Ceebeetle
{
    class WindowManager
    {
        static List<Window> m_windows = new List<Window>();

        //This all happens on the UI thread. As long as we only have one of those,
        //guarding is not needed.
        static public void OnNewWindow(Window oWnd)
        {
            m_windows.Add(oWnd);
        }
        static public void OnWindowClosing(Window oWnd)
        {
            m_windows.Remove(oWnd);
        }

        static public void CloseAll()
        {
            Window[] childWindows = new Window[m_windows.Count];
            
            m_windows.CopyTo(childWindows);
            foreach (Window oWnd in childWindows)
                oWnd.Close();
        }
    }

    public partial class CCBChildWindow : CCBWindow
    {
        public CCBChildWindow() : base()
        {
            WindowManager.OnNewWindow(this);
        }
        void OnChildWindowClosing(object sender, EventArgs evt)
        {
            WindowManager.OnWindowClosing(this);
        }
        public void Show(Window owner)
        {
            this.Owner = owner;
            this.Show();
        }
        public bool? ShowDialog(Window owner)
        {
            this.Owner = owner;
            return this.ShowDialog();
        }
    }
    public partial class CCBWindow : Window
    {
        private CCBLogger m_logger;

        public CCBWindow()
            : base()
        {
            m_logger = CCBLogConfig.GetLogger();
        }

        public void CeebeetleWindowInit()
        {
            MinWidth = Width;
            MinHeight = Height;
            if (null == this.Icon)
            {
                try
                {
                    this.Icon = ControlHelpers.NewImage("pack://application:,,,/Ceebeetle;component/Resources/app.ico");
                }
                catch (Exception ex)
                {
                    Log(ex.Message);
                }
            }
        }
        protected void SetTooltip(ContentControl ctl, string strTooltip)
        {
            if (ctl.ToolTip is string)
                ctl.ToolTip = strTooltip;
            else if (ctl.ToolTip is ToolTip)
            {
                ToolTip ttip = (ToolTip)ctl.ToolTip;

                if (null != ttip)
                    ttip.Content = strTooltip;
            }
        }
        protected bool BrowseForFile(TextBox tb)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            if (0 < tb.Text.Length)
            {
                ofd.FileName = tb.Text;
            }
            if (true == ofd.ShowDialog())
            {
                tb.Text = ofd.FileName;
                return true;
            }
            return false;
        }
        protected bool BrowseForSave(TextBox tb, bool promptOverwrite = false)
        {
            SaveFileDialog sfd = new SaveFileDialog();

            sfd.OverwritePrompt = promptOverwrite;
            if (true == sfd.ShowDialog())
            {
                tb.Text = sfd.FileName;
                return true;
            }
            return false;
        }
        protected int IntFromTextbox(TextBox ctl, System.Windows.Controls.Label txStatus)
        {
            int result = 0;
            string strNumber = ctl.Text;

            if (!System.Int32.TryParse(strNumber, out result))
                txStatus.Content = string.Format("Could not convert '{0}' to number", strNumber);
            return result;
        }
        protected void SetTextboxInt(TextBox ctl, int num)
        {
            ctl.Text = string.Format("{0}", num);
        }
        protected void SelectListboxItem(ListBox ctl, int ixSel)
        {
            if (0 < ctl.Items.Count)
            {
                if (ixSel < ctl.Items.Count)
                    ctl.SelectedIndex = ixSel;
                else
                    ctl.SelectedIndex = (ctl.Items.Count - 1);
            }
        }
        #region WndLogging
        protected void Assert(bool exp)
        {
            System.Diagnostics.Debug.Assert(exp);
        }
        protected void Log(string text)
        {
            string wndTitle = this.Title;

            if ((null == wndTitle) || (0 == wndTitle.Length))
                wndTitle = "Ceebeetle Window";
            if (null == m_logger)
                System.Diagnostics.Debug.Write(wndTitle + ":" + text);
            else
                m_logger.Log(wndTitle + ":" + text);
        }
        protected void Log(string text, int iPar)
        {
            Log(string.Format(text, iPar));
        }
        protected void Log(string text, string textPar)
        {
            Log(string.Format(text, textPar));
        }
        protected void Log(string text, string textPar1, string textPar2)
        {
            Log(string.Format(text, textPar1, textPar2));
        }
        #endregion
    }


}
