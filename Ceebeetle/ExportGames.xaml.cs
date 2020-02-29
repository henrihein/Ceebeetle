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
using Microsoft.Win32;

namespace Ceebeetle
{
    /// <summary>
    /// Interaction logic for ExportGames.xaml
    /// </summary>
    public partial class ExportGames : CCBChildWindow
    {
        private CCBGame[] m_gamesList;
        private CCBGameTemplate[] m_templateList;

        public CCBGame[] GameList
        {
            set { m_gamesList = value; }
        }
        public CCBGameTemplate[] GameTemplateList
        {
            set { m_templateList = value; }
        }

        public ExportGames()
        {
            m_gamesList = null;
            m_templateList = null;
            InitializeComponent();
            InitMinSize();
            Validate();
        }

        private void Populate()
        {
            if (null != m_gamesList)
                foreach (CCBGame game in m_gamesList)
                {
                    lbEntities.Items.Add(game);
                }
            if (null != m_templateList)
                foreach (CCBGameTemplate gTemplate in m_templateList)
                {
                    lbEntities.Items.Add(gTemplate);
                }
        }
        private void Validate()
        {
            btnExportNow.IsEnabled = (!(0 == tbTarget.Text.Length) && (0 != lbEntities.SelectedItems.Count));
        }
        private void tbTarget_TextChanged(object sender, TextChangedEventArgs e)
        {
            Validate();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();

            sfd.CheckFileExists = false;
            bool? browseRes = sfd.ShowDialog(this);

            if (true == browseRes)
            {
                tbTarget.Text = sfd.FileName;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CCBChildWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Populate();
        }

        private void btnExportNow_Click(object sender, RoutedEventArgs e)
        {
            CCBGameData gameData = new CCBGameData();

            foreach (object oEntity in lbEntities.SelectedItems)
            {
                CCBGame selectedGame = (CCBGame)oEntity;

                if (null == selectedGame)
                {
                    CCBGameTemplate gTemplate = (CCBGameTemplate)oEntity;

                    if (null != gTemplate)
                        gameData.AddSafe(gTemplate);
                }
                else
                    gameData.AddSafe(selectedGame);
            }
            if (!gameData.SaveGames(tbTarget.Text))
                tStatus.Content = "Could not save to that file.";
        }
    }
}
