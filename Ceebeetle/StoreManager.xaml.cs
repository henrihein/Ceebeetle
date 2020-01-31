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
        private bool m_initialized;

        private StoreManagerWnd()
        {
        }
        public StoreManagerWnd(CCBStoreManager manager, string storeFilePath)
        {
            m_initialized = false;
            m_manager = manager;
            InitializeComponent();
            lbPlaces.Items.Add(new CCBStorePlaceType("All"));
            tbChance.Text = "100";
            //TODO: Should really load on a background worker.
            if (m_manager.LoadStores(storeFilePath))
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
#if false
            if (0 < tbAddItem.Text.Length)
            {
                CCBStoreItemPlaceType place = GetCurrentPlaceType();
                CCBPotentialStoreItem newStoreItem = place.FindItem(tbAddItem.Text);

                if (null == newStoreItem)
                {
                    newStoreItem = new CCBPotentialStoreItem(tbAddItem.Text);
                    place.AddPotentialStoreItem(newStoreItem);
                }
                lbItems.Items.Add(newStoreItem.Item);
            }
#endif
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

            System.Diagnostics.Debug.Assert(-1 != lbPlaces.SelectedIndex);
            if (-1 == ixPlace) ixPlace = 0;
            return GetPlace(ixPlace);
        }
        private string GetCurrentItem()
        {
            System.Diagnostics.Debug.Assert(-1 != lbItems.SelectedIndex);
            if (-1 != lbItems.SelectedIndex)
            {
                object oItem = lbItems.Items[lbItems.SelectedIndex];

                return oItem.ToString();
            }
            return "none";
        }
        private void btnSaveItem_Click(object sender, RoutedEventArgs e)
        {
            bool bAvailable = true == cbItemAvailable.IsChecked;
            CCBStorePlaceType place = GetCurrentPlace();
            string itemTag = GetCurrentItem();
            CCBPotentialStoreItem potentialStoreItem = place.FindItem(itemTag);

            if (null == potentialStoreItem)
                potentialStoreItem = new CCBPotentialStoreItem(itemTag);
            potentialStoreItem.Chance = IntFromTextbox(tbChance, txStatus);
            potentialStoreItem.MinCost = IntFromTextbox(tbMinCost, txStatus);
            potentialStoreItem.MaxCost = IntFromTextbox(tbMaxCost, txStatus);
            if (0 == tbLimit.Text.Length)
                potentialStoreItem.Count = -1;
            else
                potentialStoreItem.Count = IntFromTextbox(tbLimit, txStatus);
            potentialStoreItem.RandomizeLimit = (true == cbRandomizeLimit.IsChecked);
            if (bAvailable)
                place.AddPotentialStoreItem(potentialStoreItem);
            else
                place.RemovePotentialStoreItem(potentialStoreItem);
        }
        private void cbItemAvailable_Checked(object sender, RoutedEventArgs e)
        {
            Validate();
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Reset()
        {
            tbChance.Text = "";
            tbMinCost.Text = "";
            tbMaxCost.Text = "";
            tbLimit.Text = "";
            cbItemAvailable.IsChecked = null;
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
                    cbItemAvailable.IsChecked = true;
                    SetTextboxInt(tbChance, potentialStoreItem.Chance);
                    SetTextboxInt(tbMinCost, potentialStoreItem.MinCost);
                    SetTextboxInt(tbMaxCost, potentialStoreItem.MaxCost);
                    SetTextboxInt(tbLimit, potentialStoreItem.Count);
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

        private void btnRollStore_Click(object sender, RoutedEventArgs e)
        {
            CCBStore store = m_manager.AddStore(GetCurrentPlace());
            CreateStoreWnd createStoreWnd = new CreateStoreWnd(store);

            createStoreWnd.ShowDialog();
            if (!createStoreWnd.Keep)
            {
                m_manager.DeleteStore(store);
            }
        }
    }
}
