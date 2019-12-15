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

        public NamePicker()
        {
            m_random = new Random();
            InitializeComponent();
            rbJapaneseFemale.IsEnabled = false;
            rbJapaneseMale.IsEnabled = false;
            rbElvenFemale.IsEnabled = false;
            rbElvenMale.IsEnabled = false;
            rbWesternFemale.IsChecked = true;
        }
    }
}
