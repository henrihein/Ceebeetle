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
    /// Interaction logic for BagItemPicker.xaml
    /// </summary>
    public partial class BagItemPicker : CCBChildWindow
    {
        public struct BagInfo
        {
            uint m_bagNodeId;
            CCBBag m_bag;
            CCBBag[] m_targetBags;

            public CCBBag Bag
            {
                get { return m_bag; }
            }
            public uint NodeId
            {
                get { return m_bagNodeId; }
            }
            public CCBBag[] TargetBags
            {
                get { return m_targetBags; }
            }
            public BagInfo(uint bagNodeId, CCBBag bag, CCBBag[] targetBags)
            {
                m_bagNodeId = bagNodeId;
                m_bag = bag;
                m_targetBags = targetBags;
            }
        }
        private BagInfo m_bagInfo;
        private Random m_rand;
        private int m_lastItem;
        private DOnCopyBagItems m_copyBagItemsCallback;
        private DOnDeleteBagItems m_deleteBagItemsCallback;
        public DOnCopyBagItems CopyBagItemsCallback
        {
            set { m_copyBagItemsCallback = value; }
        }
        public DOnDeleteBagItems DeleteBagItemsCallback
        {
            set { m_deleteBagItemsCallback = value; }
        }

        private BagItemPicker()
        {}
        public BagItemPicker(BagInfo bagInfo) : base()
        {
            m_bagInfo = bagInfo;
            m_rand = new Random();
            m_lastItem = -1;
            m_copyBagItemsCallback = null;
            m_deleteBagItemsCallback = null;
            InitializeComponent();
            InitMinSize();
            PopulateItems();
            CheckBagItems();
            cbSelectionMode.SelectedIndex = 0;
            string strDeleteButtonTooltip = btnDelete.ToolTip.ToString();
            SetTooltip(btnDelete, string.Format(strDeleteButtonTooltip, m_bagInfo.Bag.Name));            
        }

        private void CheckBagItems()
        {
            btnPickNow.IsEnabled = !lbBagItems.Items.IsEmpty;
            btnPickAllSelected.IsEnabled = 0 < lbBagItems.SelectedItems.Count;
            btnUndo.IsEnabled = (-1 != m_lastItem);
            btnCopy.IsEnabled = (!lbPickedItems.Items.IsEmpty && (-1 != lbTargetBag.SelectedIndex) && (null != m_copyBagItemsCallback));
            btnDelete.IsEnabled = !lbPickedItems.Items.IsEmpty && (null != m_deleteBagItemsCallback);
        }
        private void PopulateItems()
        {
            System.Diagnostics.Debug.Assert(null != m_bagInfo.Bag);
            if (null != m_bagInfo.Bag)
            {
                foreach (CCBBagItem bagItem in m_bagInfo.Bag.Items)
                {
                    lbBagItems.Items.Add(bagItem.ToString());
                }
            }
            System.Diagnostics.Debug.Assert(null != m_bagInfo.TargetBags);
            if (null != m_bagInfo.TargetBags)
            {
                foreach (CCBBag bag in m_bagInfo.TargetBags)
                    lbTargetBag.Items.Add(bag);
            }
        }
        private CCBBag GetTargetBag()
        {
            if (-1 != lbTargetBag.SelectedIndex)
                return (CCBBag)lbTargetBag.SelectedItem;
            return null;
        }
        
        private void OnClosePicker(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void PickFromAllItems()
        {
            if (!lbBagItems.Items.IsEmpty)
            {
                int ixItem = m_rand.Next(lbBagItems.Items.Count);
                string item;

                System.Diagnostics.Debug.Assert((0 <= ixItem) && (lbBagItems.Items.Count > ixItem));
                item = lbBagItems.Items[ixItem].ToString();
                lbBagItems.Items.RemoveAt(ixItem);
                m_lastItem = lbPickedItems.Items.Add(item);
            }
        }
        private void PickFromSelectedItems()
        {
            if ((null != lbBagItems.SelectedItems) && (0 < lbBagItems.SelectedItems.Count))
            {
                int ixItem = m_rand.Next(lbBagItems.SelectedItems.Count);
                string item;

                System.Diagnostics.Debug.Assert((0 <= ixItem) && (lbBagItems.SelectedItems.Count > ixItem));
                item = lbBagItems.SelectedItems[ixItem].ToString();
                lbBagItems.Items.Remove(item);
                m_lastItem = lbPickedItems.Items.Add(item);
            }
        }
        private void PickFromUnselectedItems()
        {
            if ((null != lbBagItems.SelectedItems) && (0 < lbBagItems.Items.Count) && (0 <= lbBagItems.SelectedItems.Count))
            {
                int ixItem = m_rand.Next(lbBagItems.Items.Count - lbBagItems.SelectedItems.Count);
                int cLooked = 0, cSelected = 0;
                object item = null;

                System.Diagnostics.Debug.Assert(lbBagItems.Items.Count >= lbBagItems.SelectedItems.Count);
                System.Diagnostics.Debug.Assert(0 <= ixItem);
                //Dunno if there is a better way to find the item than to walk the list(s)
                for (int ixLook = 0; ixLook < lbBagItems.Items.Count; ixLook++)
                {
                    if (lbBagItems.SelectedItems.Contains(lbBagItems.Items[ixLook]))
                        cSelected++;
                    else if (cLooked == ixItem)
                    {
                        item = lbBagItems.Items[ixLook];
                        break;
                    }
                    else
                        cLooked++;
                }
                if (null != item)
                {
                    lbBagItems.Items.Remove(item);
                    m_lastItem = lbPickedItems.Items.Add(item);
                }
            }
        }
        private void btnPickNow_Click(object sender, RoutedEventArgs e)
        {
            switch (cbSelectionMode.SelectedIndex)
            {
                case 0:
                    PickFromAllItems();
                    break;
                case 1:
                    PickFromSelectedItems();
                    break;
                case 2:
                    PickFromUnselectedItems();
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
            CheckBagItems();
        }
        private void btnPickAllSelected_Click(object sender, RoutedEventArgs e)
        {
            if ((null != lbBagItems.SelectedItems) && (0 < lbBagItems.SelectedItems.Count))
            {
                object[] selItems = new object[lbBagItems.SelectedItems.Count];

                for (int ix = 0; ix < lbBagItems.SelectedItems.Count; ix++)
                    selItems[ix] = lbBagItems.SelectedItems[ix];
                foreach (object oItem in selItems)
                {
                    lbBagItems.Items.Remove(oItem);
                    m_lastItem = lbPickedItems.Items.Add(oItem.ToString());
                }
            }
        }

        private void OnUndoPick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.Assert(-1 != m_lastItem);
            if (-1 != m_lastItem)
            {
                object pickedItem = lbPickedItems.Items[m_lastItem];

                if (null != pickedItem)
                {
                    lbPickedItems.Items.Remove(pickedItem);
                    lbBagItems.Items.Add(pickedItem.ToString());
                }
            }
            m_lastItem = -1;
            CheckBagItems();
        }

        private void OnReset(object sender, RoutedEventArgs e)
        {
            List<object> items = new List<object>();

            foreach (object oItem in lbPickedItems.Items)
                items.Add(oItem);
            foreach (string strItem in items)
            {
                lbBagItems.Items.Add(strItem.ToString());
                lbPickedItems.Items.Remove(strItem);
            }
            m_lastItem = -1;
            CheckBagItems();
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (null != m_copyBagItemsCallback)
            {
                CCBBag targetBag = GetTargetBag();

                if (null != targetBag)
                {
                    string[] itemsToCopy = new string[lbPickedItems.Items.Count];

                    lbPickedItems.Items.CopyTo(itemsToCopy, 0);
                    m_copyBagItemsCallback(targetBag, itemsToCopy);
                }
            }
        }
        private void lbBagItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckBagItems();
        }
        private void lbTargetBag_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnCopy.IsEnabled = (!lbPickedItems.Items.IsEmpty && (-1 != lbTargetBag.SelectedIndex) && (null != m_copyBagItemsCallback));
        }
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (null != m_deleteBagItemsCallback)
            {
                string strConfirmMessage = string.Format("{0} items will be deleted from {1}. This cannot be reverted. Click 'Yes' to continue.",
                                                            lbPickedItems.Items.Count, m_bagInfo.Bag.ToString());
                MessageBoxResult mbr = System.Windows.MessageBox.Show(strConfirmMessage, "Confirm Item Deletion", MessageBoxButton.YesNo);

                if (MessageBoxResult.Yes == mbr)
                {
                    string[] itemsToCopy = new string[lbPickedItems.Items.Count];

                    lbPickedItems.Items.CopyTo(itemsToCopy, 0);
                    if (m_deleteBagItemsCallback(m_bagInfo.Bag, itemsToCopy))
                        lbPickedItems.Items.Clear();
                }
            }
        }
    }
}
