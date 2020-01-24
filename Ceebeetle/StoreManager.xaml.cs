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
    public partial class StoreManager : CCBChildWindow
    {
        private CCBStoreItemPlaceTypeList m_placeList;
        private bool m_initialized;

        public StoreManager()
        {
            m_initialized = false;
            m_placeList = new CCBStoreItemPlaceTypeList();
            InitializeComponent();
            lbPlaces.Items.Add(new CCBStoreItemPlaceType("All"));
            tbChance.Text = "100";
            Validate();
            m_initialized = true;
        }

        private void Validate()
        {
            bool itemAvailable = true == cbItemAvailable.IsChecked;

            btnAddPlace.IsEnabled = 0 < tbPlace.Text.Length;
            btnAddItem.IsEnabled = 0 < tbAddItem.Text.Length;
            btnSaveItem.IsEnabled = true; // (0 < lbItems.SelectedItems.Count) && (0 < tbMaxCost.Text.Length) && (0 < tbMinCost.Text.Length) && (0 < tbChance.Text.Length);
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
                lbPlaces.Items.Add(new CCBStoreItemPlaceType(tbPlace.Text));
        }

        private void OnItemText_Changed(object sender, TextChangedEventArgs e)
        {
            Validate();
        }
        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.Assert(0 < tbAddItem.Text.Length);
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
        }
        private CCBStoreItemPlaceType GetCurrentPlaceType()
        {
            System.Diagnostics.Debug.Assert(-1 == lbPlaces.SelectedIndex);
            if (-1 == lbPlaces.SelectedIndex)
            {
                if (0 == lbPlaces.Items.Count)
                {
                    System.Diagnostics.Debug.Assert(false);
                    lbPlaces.Items.Add(new CCBStoreItemPlaceType("All"));
                }
                return (CCBStoreItemPlaceType)lbPlaces.Items[0];
            }
            return (CCBStoreItemPlaceType)lbPlaces.Items[lbPlaces.SelectedIndex];
        }
        private void btnSaveItem_Click(object sender, RoutedEventArgs e)
        {
            bool bAvailable = true == cbItemAvailable.IsChecked;
            CCBStoreItemPlaceType place = GetCurrentPlaceType();

            foreach (object oItem in lbItems.SelectedItems)
            {
                CCBPotentialStoreItem potentialStoreItem = place.FindItem(oItem.ToString());

                if (null == potentialStoreItem)
                    potentialStoreItem = new CCBPotentialStoreItem(oItem.ToString());
                potentialStoreItem.Chance = IntFromTextbox(tbChance, txStatus);
                potentialStoreItem.MinCost = IntFromTextbox(tbMinCost, txStatus);
                potentialStoreItem.MaxCost = IntFromTextbox(tbMaxCost, txStatus);
                if (0 == tbLimit.Text.Length)
                    potentialStoreItem.Limit = -1;
                else
                    potentialStoreItem.Limit = IntFromTextbox(tbLimit, txStatus);
                potentialStoreItem.RandomizeLimit = (true == cbRandomizeLimit.IsChecked);
                if (bAvailable)
                    place.AddPotentialStoreItem(potentialStoreItem);
                else
                    place.RemovePotentialStoreItem(potentialStoreItem);
            }
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
            CCBStoreItemPlaceType place = GetCurrentPlaceType();
            CCBPotentialStoreItem prevItem = null;

            foreach (object oItem in lbItems.SelectedItems)
            {
                CCBPotentialStoreItem potentialStoreItem = place.FindItem(oItem.ToString());

                if (null != potentialStoreItem)
                {
                    if (null == prevItem)
                    {
                        prevItem = potentialStoreItem;
                        SetTextboxInt(tbChance, potentialStoreItem.Chance);
                        SetTextboxInt(tbMinCost, potentialStoreItem.MinCost);
                        SetTextboxInt(tbMaxCost, potentialStoreItem.MaxCost);
                        SetTextboxInt(tbLimit, potentialStoreItem.Limit);
                        cbRandomizeLimit.IsChecked = potentialStoreItem.RandomizeLimit;
                    }
                    else if (!prevItem.CompareStats(potentialStoreItem))
                    {
                        Reset();
                        return;
                    }
                }
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
    }
}
