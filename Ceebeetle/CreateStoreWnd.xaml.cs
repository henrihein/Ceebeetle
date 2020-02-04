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
    /// Interaction logic for CreateStoreWnd.xaml
    /// </summary>
    public partial class CreateStoreWnd : CCBChildWindow
    {
        private CCBStore m_store;
        private bool m_keepStore;

        public bool Keep
        {
            get { return m_keepStore; }
        }

        public CreateStoreWnd(CCBStore store)
        {
            m_store = store;
            m_keepStore = false;
            InitializeComponent();
            tbStoreName.Text = store.Name;
            btnDeleteItem.IsEnabled = false;
            Populate();
        }
        private void AddStoreItem(CCBStoreItem storeItem)
        {
            lbItems.Items.Add(storeItem);
        }
        private void AddOmittedItem(CCBStoreItemOmitted item)
        {
            lbUnavailable.Items.Add(item);
        }
        private void Populate()
        {
            lStoreType.Content = "In: " + m_store.StoreType;
            foreach (CCBBagItem item in m_store.Items)
            {
                if (item is CCBStoreItemOmitted)
                    AddOmittedItem((CCBStoreItemOmitted)item);
                else if (item is CCBStoreItem)
                    AddStoreItem((CCBStoreItem)item);
                else
                {
                    System.Diagnostics.Debug.Write(string.Format("Unexpected object in store list: {0}", item.GetType().ToString()));
                    System.Diagnostics.Debug.Assert(false);
                }
            }
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        CCBStoreItem GetCurrentItem()
        {
            System.Diagnostics.Debug.Assert(-1 != lbItems.SelectedIndex);
            if (-1 != lbItems.SelectedIndex)
                return (CCBStoreItem)lbItems.Items[lbItems.SelectedIndex];
            return null;
        }
        private void lbItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CCBStoreItem selItem = GetCurrentItem();

            if (null != selItem)
            {
                SetTextboxInt(tbCost, selItem.Cost);
                if (-1 == selItem.Count)
                {
                    labelLimit.Visibility = Visibility.Hidden;
                    tbLimit.Visibility = Visibility.Hidden;
                }
                else
                {
                    labelLimit.Visibility = Visibility.Visible;
                    tbLimit.Visibility = Visibility.Visible;
                    SetTextboxInt(tbLimit, selItem.Count);
                }
                btnDeleteItem.IsEnabled = true;
            }
            else
                btnDeleteItem.IsEnabled = false;
        }
        private void Save()
        {
            CCBStoreItem item = GetCurrentItem();

            if (null != item)
            {
                item.Cost = IntFromTextbox(tbCost, lbStatus);
                if (tbLimit.IsVisible)
                    item.Count = IntFromTextbox(tbLimit, lbStatus);
            }
        }
        private void tbCost_LostFocus(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void btnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            CCBStoreItem item = GetCurrentItem();

            if (null != item)
            {
                int ixCur = lbItems.SelectedIndex;

                m_store.RemoveItem(item);
                lbItems.Items.RemoveAt(ixCur);
                SelectListboxItem(lbItems, ixCur);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            m_keepStore = false;
            Close();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            m_keepStore = true;
            m_store.Name = tbStoreName.Text;
            Close();
        }
    }
}
