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
    /// Interaction logic for NamePicker.xaml
    /// </summary>
    public partial class NamePicker : CCBChildWindow
    {
        private Random m_random;
        private Names.CharacterNameGenerators m_nameGenerators;

        public NamePicker()
        {
            m_random = new Random();
            m_nameGenerators = new Names.CharacterNameGenerators();
            InitializeComponent();
            rbJapaneseFemale.IsEnabled = false;
            rbJapaneseMale.IsEnabled = false;
            rbElvenFemale.IsEnabled = false;
            rbElvenMale.IsEnabled = false;
            rbWesternFemale.IsChecked = true;
            Validate();
        }

        private void Validate()
        {
            btnDelete.IsEnabled = (-1 != lbPicked.SelectedIndex);
        }

        private Names.CharacterNames GetCharacterNameGenerator()
        {
            if (true == rbWesternFemale.IsChecked)
                return m_nameGenerators.GetWesternFemaleNameGenerator();
            if (true == rbWesternMale.IsChecked)
                return m_nameGenerators.GetWesternMaleNameGenerator();
            return null;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnPick_Click(object sender, RoutedEventArgs e)
        {
            Names.CharacterNames nameGenerator = GetCharacterNameGenerator();

            if (null != nameGenerator)
                lbPicked.Items.Add(nameGenerator.GetRandomName(m_random));
        }

        private void lbPicked_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Validate();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            lbPicked.Items.Clear();
        }
    }
}
