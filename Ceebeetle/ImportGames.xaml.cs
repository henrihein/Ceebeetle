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
using System.IO;
using Microsoft.Win32;

namespace Ceebeetle
{
    /// <summary>
    /// Interaction logic for ImportGames.xaml
    /// </summary>
    public partial class ImportGames : CCBChildWindow
    {
        DMergeGame m_mergeGameCallback = null;
        DMergeTemplate m_mergeTemplateCallback = null;
        CCBGameData m_games = null;

        private ImportGames()
        {
        }
        public ImportGames(DMergeGame mergeGameCallback, DMergeTemplate mergeTemplateCallback)
        {
            m_mergeGameCallback = mergeGameCallback;
            m_mergeTemplateCallback = mergeTemplateCallback;
            m_games = new CCBGameData();
            InitializeComponent();
            Validate();
        }

        private void Validate()
        {
            if ((0 < tbSource.Text.Length) && (File.Exists(tbSource.Text)))
                btnView.IsEnabled = true;
            else
                btnView.IsEnabled = false;
            if (-1 == lbGames.SelectedIndex)
                btnMerge.IsEnabled = false;
            else
            {
                btnMerge.IsEnabled = true;
                if (lbGames.Items[lbGames.SelectedIndex] is CCBGame)
                    btnMerge.Content = "_Merge game";
                else if (lbGames.Items[lbGames.SelectedIndex] is CCBGameTemplate)
                    btnMerge.Content = "_Merge template";
                else
                {
                    tStatus.Content = "Unknown item in view list";
                    btnMerge.Content = "_Merge";
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();

            ofd.CheckFileExists = true;
            bool? browseRes = ofd.ShowDialog(this);

            if (true == browseRes)
            {
                tbSource.Text = ofd.FileName;
            }
        }
        private void lbGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Validate();
        }
        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (TStatusUpdate.tsuFileLoaded == m_games.LoadGamesSafe(tbSource.Text))
            {
                CCBGame[] games = m_games.GetGames();
                CCBGameTemplate[] templates = m_games.GetGameTemplates();

                foreach (CCBGame game in games)
                    lbGames.Items.Add(game);
                foreach (CCBGameTemplate template in templates)
                    lbGames.Items.Add(template);
            }
        }
        private void tbSource_TextChanged(object sender, TextChangedEventArgs e)
        {
            Validate();
        }

        private void btnMerge_Click(object sender, RoutedEventArgs e)
        {
            TStatusUpdate tsu = TStatusUpdate.tsuNone;

            System.Diagnostics.Debug.Assert(null != m_mergeGameCallback);
            System.Diagnostics.Debug.Assert(null != m_mergeTemplateCallback);
            if (-1 != lbGames.SelectedIndex)
            {
                if (lbGames.Items[lbGames.SelectedIndex] is CCBGame)
                {
                    CCBGame game = (CCBGame)lbGames.Items[lbGames.SelectedIndex];

                    if (null != game)
                        tsu = m_mergeGameCallback(game);
                }
                if (lbGames.Items[lbGames.SelectedIndex] is CCBGameTemplate)
                {
                    CCBGameTemplate template = (CCBGameTemplate)lbGames.Items[lbGames.SelectedIndex];

                    if (null != template)
                        tsu = m_mergeTemplateCallback(template);
                }
                if (TStatusUpdate.tsuFileLoaded == tsu)
                    tStatus.Content = "";
                else
                    tStatus.Content = "Could not load item.";
            }
            else
                tStatus.Content = "Wrong item in list.";
        }
    }
}
