using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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

    public partial class CCBChildWindow : Window
    {
        public CCBChildWindow()
            : base()
        {
            WindowManager.OnNewWindow(this);
        }

        void OnChildWindowClosing(object sender, EventArgs evt)
        {
            WindowManager.OnWindowClosing(this);
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
    }
}
