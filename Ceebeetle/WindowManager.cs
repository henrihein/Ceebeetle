﻿using System;
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
    }
}
