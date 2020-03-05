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
    /// Interaction logic for StorePickerWnd.xaml
    /// </summary>
    public partial class StorePickerWnd : CCBChildWindow
    {
        public DStorePicked StorePickedCallback
        {
            get; set;
        }

        public StorePickerWnd(List<CCBStore> stores)
        {
            InitializeComponent();
            CeebeetleWindowInit();
            PopulateStoreList(stores);
        }

        private void PopulateStoreList(List<CCBStore> stores)
        {
            foreach (CCBStore store in stores)
            {
                lbStores.Items.Add(store);
            }
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
