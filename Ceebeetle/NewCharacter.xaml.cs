using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
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
    /// Interaction logic for NewCharacter.xaml
    /// </summary>
    public partial class NewCharacter : Window
    {
        private readonly OnNewCharacter m_onAdded;
        private bool m_shutdown;

        private NewCharacter()
        {
            m_shutdown = false;
        }
        public NewCharacter(OnNewCharacter updater)
        {
            m_shutdown = false;
            m_onAdded = updater;
            InitializeComponent();
        }

        public void SetShutdown()
        {
            m_shutdown = true;
        }

        void NewCharacter_Closing(object sender, CancelEventArgs evtArgs)
        {
            if (!m_shutdown)
                evtArgs.Cancel = true;
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
        private void btnAddCharacter_Click(object sender, RoutedEventArgs e)
        {
            CCBCharacter newCharacter = new CCBCharacter(tbCharacterName.Text);

            m_onAdded(newCharacter);
        }

        private void tbCharacterName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbCharacterName.Text.Equals(""))
                btnAddCharacter.IsEnabled = false;
            else
                btnAddCharacter.IsEnabled = true;
        }
    }
}
