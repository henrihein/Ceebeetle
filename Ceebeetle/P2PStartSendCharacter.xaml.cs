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
    /// Interaction logic for P2PStartSendCharacter.xaml
    /// </summary>
    public partial class P2PStartSendCharacter : CCBChildWindow
    {
        private CCBCharacter m_character;
        private string m_recipient;

        public string Recipient
        {
            get { return m_recipient; }
        }
        public CCBCharacter Character
        {
            get { return m_character; }
        }
        public P2PStartSendCharacter(string[] users, CCBGameData gameData)
        {
            m_character = null;
            InitializeComponent();
            Populate(users, gameData);
            Validat();
        }
        private void Populate(string[] users, CCBGameData gameData)
        {
            if (null != users) foreach (string user in users)
                    lbUsers.Items.Add(user);
            foreach (CCBGame game in gameData)
            {
                foreach (CCBCharacter character in game.Characters)
                {
                    lbCharacters.Items.Add(new CCBCharacterContainer(game, character));
                }
            }
        }
        public void Validat()
        {
            btnSend.IsEnabled = ((-1 != lbCharacters.SelectedIndex) &&
                                    (-1 != lbUsers.SelectedIndex));
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        private void lbCharacters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Validat();
        }
        private void lbUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Validat();
        }
        private CCBCharacter GetSelectedCharacter()
        {
            if (-1 != lbCharacters.SelectedIndex)
            {
                CCBCharacterContainer characterContainerObj = (CCBCharacterContainer)lbCharacters.Items[lbCharacters.SelectedIndex];

                if (null != characterContainerObj)
                    return characterContainerObj.Character;
                Log("Error: Non-CharacterContainer in lbCharacters");
            }
            return null;
        }
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            CCBCharacter character = GetSelectedCharacter();

            Assert(-1 != lbUsers.SelectedIndex);
            if ((-1 != lbUsers.SelectedIndex) && (null != character))
            {
                m_recipient = lbUsers.Items[lbUsers.SelectedIndex].ToString();
                m_character = character;
                DialogResult = true;
            }
            else
                DialogResult = false;
        }

    }
}
