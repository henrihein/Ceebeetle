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
    /// Interaction logic for StoreViewerWnd.xaml
    /// </summary>
    public partial class StoreViewerWnd : CCBChildWindow
    {
        private CCBStoreManager m_storeMgr;

        public StoreViewerWnd(CCBStoreManager storeMgr)
        {
            m_storeMgr = storeMgr;
            InitializeComponent();
            Populate();
        }

        private void Populate()
        {
            foreach (CCBStore store in m_storeMgr.Stores)
            {
                lbStores.Items.Add(store);
            }
        }
        private CCBStore GetCurrentStore()
        {
            if (-1 != lbStores.SelectedIndex)
                return (CCBStore)lbStores.Items[lbStores.SelectedIndex];
            return null;
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            CCBStore curStore = GetCurrentStore();

            if (null != curStore)
            {
                int ixCur = lbStores.SelectedIndex;

                lbStores.Items.RemoveAt(ixCur);
                SelectListboxItem(lbStores, ixCur);
            }
        }
    }
}
