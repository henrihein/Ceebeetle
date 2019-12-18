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
        private DOnCopyName m_copyNameCallback;

        public DOnCopyName CopyNameCallback
        {
            set { m_copyNameCallback = value; }
        }

        public NamePicker()
        {
            m_random = new Random();
            m_nameGenerators = new Names.CharacterNameGenerators();
            InitializeComponent();
            rbWesternFemale.IsChecked = true;
            Validate();
        }

        private void Validate()
        {
            btnDelete.IsEnabled = (-1 != lbPicked.SelectedIndex);
            btnCopy.IsEnabled = (-1 != lbPicked.SelectedIndex);
        }

        private Names.CharacterNames GetCharacterNameGenerator()
        {
            if (true == rbWesternFemale.IsChecked)
                return m_nameGenerators.GetWesternFemaleNameGenerator();
            if (true == rbWesternMale.IsChecked)
                return m_nameGenerators.GetWesternMaleNameGenerator();
            if (true == rbJapaneseFemale.IsChecked)
                return m_nameGenerators.GetJapaneseFemaleNameGenerator();
            if (true == rbJapaneseMale.IsChecked)
                return m_nameGenerators.GetJapaneseMaleNameGenerator();
            if (true == rbElvenFemale.IsChecked)
                return m_nameGenerators.GetElvenFemaleNameGenerator();
            if (true == rbElvenMale.IsChecked)
                return m_nameGenerators.GetElvenMaleNameGenerator();
            if (true == rbNordicDwarven.IsChecked)
                return m_nameGenerators.GetNordicDwarvenNameGenerator();
            if (true == rbTolkienDwarven.IsChecked)
                return m_nameGenerators.GetTolkienDwarvenNameGenerator();
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
        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            Names.CharacterNames nameGenerator = GetCharacterNameGenerator();

            if (null != nameGenerator)
                lbPicked.Items.Add(nameGenerator.GenerateRandomName(m_random));
       }
        private void lbPicked_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Validate();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            lbPicked.Items.Clear();
        }
        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            if ((null != m_copyNameCallback) && (-1 != lbPicked.SelectedIndex))
                m_copyNameCallback(lbPicked.SelectedItem.ToString());
        }
        private void btnCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (-1 != lbPicked.SelectedIndex)
                Clipboard.SetText(lbPicked.SelectedItem.ToString());
        }

    }
}
