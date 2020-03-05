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
            CeebeetleWindowInit();
            Populate();
            Validate();
        }

        private void Populate()
        {
            foreach (CCBStore store in m_storeMgr.Stores)
            {
                lbStores.Items.Add(store);
            }
        }
        private void PopulateItems()
        {
            CCBStore store = GetCurrentStore();

            lbItems.Items.Clear();
            if (null != store)
            {
                foreach (CCBBagItem item in store.Items)
                {
                    if (item is CCBStoreItem)
                        lbItems.Items.Add(new StoreItemViewer(item));
                }
            }
        }
        private CCBStore GetCurrentStore()
        {
            if (-1 == lbStores.SelectedIndex)
                return null;
            return (CCBStore)lbStores.Items[lbStores.SelectedIndex];
        }
        private void Validate()
        {
            btnDeleteItem.IsEnabled = (-1 != lbItems.SelectedIndex);
            btnDeleteStore.IsEnabled = (-1 != lbStores.SelectedIndex);
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

        private void lbItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Validate();
        }
        private void lbStores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PopulateItems();
            Validate();
        }

        private void btnDeleteStore_Click(object sender, RoutedEventArgs e)
        {
            CCBStore curStore = GetCurrentStore();

            if (null != curStore)
            {
                int ixCur = lbStores.SelectedIndex;

                lbStores.Items.RemoveAt(ixCur);
                SelectListboxItem(lbStores, ixCur);
                m_storeMgr.DeleteStore(curStore);
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDlg = new PrintDialog();
            CCBStore store = GetCurrentStore();

            if (null != store)
            {
                if (true != printDlg.ShowDialog())
                    return;
                else
                {
                    Visual oPrint = store.Print();

                    printDlg.PrintVisual(oPrint, store.ToString());
                }
            }
        }
    }
}
