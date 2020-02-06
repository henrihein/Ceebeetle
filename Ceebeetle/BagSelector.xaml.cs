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
    /// Interaction logic for BagSelector.xaml
    /// </summary>
    public partial class BagSelector : CCBChildWindow
    {
        private CCBGame m_game;
        private CCBBag m_bag;

        public CCBBag SelectedBag
        {
            get { return m_bag; }
        }

        private BagSelector()
        {
        }
        public BagSelector(CCBGame game)
        {
            m_game = game;
            m_bag = null;
            InitializeComponent();
            Populate();
            Validate();
        }
        private void Populate()
        {
            if (null != m_game)
            {
                CCBBag[] bags = m_game.GetAllBags(null);

                foreach (CCBBag bag in bags)
                    lbBags.Items.Add(bag);
            }
        }
        private void Validate()
        {
            btnSelect.IsEnabled = (-1 != lbBags.SelectedIndex);
        }

        private void lbBags_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Validate();
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (-1 != lbBags.SelectedIndex)
                m_bag = (CCBBag)lbBags.Items[lbBags.SelectedIndex];
            DialogResult = true;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            m_bag = null;
            DialogResult = false;
            Close();
        }
    }
}
