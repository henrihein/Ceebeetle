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
    /// Interaction logic for StoreManager.xaml
    /// </summary>
    public partial class StoreManagerWnd : CCBChildWindow
    {
        private CCBStoreManager m_manager;
        private CCBGame m_game;
        private bool m_initialized;

        private StoreManagerWnd()
        {
            m_manager = null;
            m_game = null;
        }
        public StoreManagerWnd(CCBStoreManager manager, CCBGame game)
        {
            m_initialized = false;
            m_manager = manager;
            m_game = game;
            InitializeComponent();
            CeebeetleWindowInit();
            tbChance.Text = "100";
            Populate();
            Validate();
            m_initialized = true;
        }

        private void Populate()
        {
            HashSet<string> itemSet = new HashSet<string>();

            foreach (CCBStorePlaceType place in m_manager.Places)
            {
                lbPlaces.Items.Add(place);
                foreach (CCBBagItem item in place.StoreItems.Items)
                {
                    itemSet.Add(item.Item);
                }
            }
            foreach (string item in itemSet)
                lbItems.Items.Add(item);
        }
        private void Validate()
        {
            bool itemAvailable = true == cbItemAvailable.IsChecked;

            btnAddPlace.IsEnabled = 0 < tbPlace.Text.Length;
            btnAddItem.IsEnabled = 0 < tbAddItem.Text.Length;
            btnAddItemsFromBag.IsEnabled = (null != m_game);
            btnSaveItem.IsEnabled = true;
            tbMinCost.IsEnabled = itemAvailable;
            tbMaxCost.IsEnabled = itemAvailable;
            tbChance.IsEnabled = itemAvailable;
            tbLimit.IsEnabled = itemAvailable;
            cbRandomizeLimit.IsEnabled = itemAvailable;
            if (-1 == lbPlaces.SelectedIndex)
                lbPlaces.SelectedIndex = 0;
        }
        private void tbPlace_OnChanged(object sender, TextChangedEventArgs e)
        {
            Validate();
        }

        private void btnAddPlace_OnClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.Assert(0 < tbPlace.Text.Length);
            if (0 < tbPlace.Text.Length)
            {
                CCBStorePlaceType newPlaceType = m_manager.AddPlaceType(tbPlace.Text);

                lbPlaces.Items.Add(newPlaceType);
                tbPlace.Text = "";
            }
        }
        private void OnItemText_Changed(object sender, TextChangedEventArgs e)
        {
            Validate();
        }
        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.Assert(0 < tbAddItem.Text.Length);
            lbItems.Items.Add(tbAddItem.Text);
            tbAddItem.Text = "";
        }
        private void btnAddItemsFromBag_Click(object sender, RoutedEventArgs e)
        {
            BagSelector bagSelector = new BagSelector(m_game);

            if (true == bagSelector.ShowDialog())
            {
                if (null != bagSelector.SelectedBag)
                {
                    foreach (CCBBagItem item in bagSelector.SelectedBag.Items)
                        lbItems.Items.Add(item.Item);
                }
            }
        }
        private CCBStorePlaceType GetPlace(int ixPlace)
        {
            CCBStorePlaceType place = null;

            place = (CCBStorePlaceType)lbPlaces.Items[ixPlace];
            if (null != place)
                return place;
            System.Diagnostics.Debug.Assert(false);
            System.Diagnostics.Debug.Write(string.Format("Unknown item in place list at {0}", ixPlace));
            throw new CEStoreManagerNoPlaceFound("Unknown item in place list at {0}", ixPlace);
        }
        private CCBStorePlaceType GetCurrentPlace()
        {
            int ixPlace = lbPlaces.SelectedIndex;

            if (-1 == ixPlace)
            {
                if (0 == lbPlaces.Items.Count)
                    return null;
                ixPlace = 0;
            }
            return GetPlace(ixPlace);
        }
        private string GetCurrentItem()
        {
            if (-1 != lbItems.SelectedIndex)
            {
                object oItem = lbItems.Items[lbItems.SelectedIndex];

                return oItem.ToString();
            }
            return null;
        }
        private void btnSaveItem_Click(object sender, RoutedEventArgs e)
        {
            SaveItem();
        }
        private void SaveItem()
        {
            bool bAvailable = true == cbItemAvailable.IsChecked;
            CCBStorePlaceType place = GetCurrentPlace();
            string itemTag = GetCurrentItem();

            if ((null != place) && (null != itemTag))
            {
                CCBPotentialStoreItem potentialStoreItem = place.FindItem(itemTag);

                if (null == potentialStoreItem)
                    potentialStoreItem = new CCBPotentialStoreItem(itemTag);
                potentialStoreItem.Available = bAvailable;
                potentialStoreItem.Chance = IntFromTextbox(tbChance, txStatus);
                potentialStoreItem.MinCost = IntFromTextbox(tbMinCost, txStatus);
                potentialStoreItem.MaxCost = IntFromTextbox(tbMaxCost, txStatus);
                if (true == cbLimit.IsChecked)
                    potentialStoreItem.Count = IntFromTextbox(tbLimit, txStatus);
                else
                    potentialStoreItem.Count = -1;
                potentialStoreItem.RandomizeLimit = (true == cbRandomizeLimit.IsChecked);
                place.AddPotentialStoreItem(potentialStoreItem);
            }
        }
        private void cbItemAvailable_Checked(object sender, RoutedEventArgs e)
        {
            Validate();
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            SaveItem();
            this.Close();
        }

        private void Reset()
        {
            tbChance.Text = "";
            tbMinCost.Text = "";
            tbMaxCost.Text = "";
            tbLimit.Text = "";
            cbItemAvailable.IsChecked = false;
        }
        private void UpdateProperties()
        {
            string itemTag = GetCurrentItem();
            CCBStorePlaceType place = GetCurrentPlace();

            System.Diagnostics.Debug.Assert(null != place);
            if (null != place)
            {
                CCBPotentialStoreItem potentialStoreItem = place.FindItem(itemTag);

                if (null != potentialStoreItem)
                {
                    cbItemAvailable.IsChecked = potentialStoreItem.Available;
                    SetTextboxInt(tbChance, potentialStoreItem.Chance);
                    SetTextboxInt(tbMinCost, potentialStoreItem.MinCost);
                    SetTextboxInt(tbMaxCost, potentialStoreItem.MaxCost);
                    if (-1 == potentialStoreItem.Count)
                    {
                        cbLimit.IsChecked = false;
                        tbLimit.Text = "";
                        tbLimit.IsEnabled = false;
                    }
                    else
                    {
                        cbLimit.IsChecked = true;
                        SetTextboxInt(tbLimit, potentialStoreItem.Count);
                        tbLimit.IsEnabled = true;
                    }
                    cbRandomizeLimit.IsChecked = potentialStoreItem.RandomizeLimit;
                }
                else
                    Reset();
            }
        }
        private void lbPlaces_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_initialized) UpdateProperties();
        }
        private void lbItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_initialized) UpdateProperties();
        }
        private void cbLimit_Checked(object sender, RoutedEventArgs e)
        {
            tbLimit.IsEnabled = true == cbLimit.IsChecked;
        }

        private void btnRollStore_Click(object sender, RoutedEventArgs e)
        {
            CCBStore store = m_manager.AddStore(GetCurrentPlace());
            CreateStoreWnd createStoreWnd = new CreateStoreWnd(store);

            SaveItem();
            createStoreWnd.ShowDialog();
            if (!createStoreWnd.Keep)
            {
                m_manager.DeleteStore(store);
            }
        }

        private void tbChance_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveItem();
        }
        private void tbMinCost_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveItem();
        }
        private void tbMaxCost_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveItem();
        }
        private void tbLimit_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveItem();
        }
        private void cbRandomizeLimit_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveItem();
        }
        private void cbLimit_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveItem();
        }
        private void cbItemAvailable_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveItem();
        }

        private void btnViewStores_Click(object sender, RoutedEventArgs e)
        {
            StoreViewerWnd viewerWnd = new StoreViewerWnd(m_manager);

            SaveItem();
            viewerWnd.ShowDialog();
        }
    }
}
