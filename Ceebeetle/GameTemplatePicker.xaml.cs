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
    /// Interaction logic for GameTemplatePicker.xaml
    /// </summary>
    public partial class GameTemplatePicker : CCBChildWindow
    {
        private CCBGame m_model;
        private readonly string m_modelName;
        private readonly DOnCreateNewGame m_gameCreateCallback;
        private readonly DOnCreateNewTemplate m_templateCreateCallback;

        public GameTemplatePicker(CCBGame gameModel, DOnCreateNewGame newGameCallback, DOnCreateNewTemplate newTemplateCallback, CCBGameTemplateList userList)
        {
            InitializeComponent();
            InitMinSize();
            if (null == gameModel)
            {
                m_modelName = InitializeNewGameButtonText("Game");
                btnAddTemplate.IsEnabled = false;
            }
            else
            {
                m_modelName = InitializeNewGameButtonText(gameModel.Name);
                m_model = gameModel;
                tbName.Text = gameModel.Name;
            }
            btnAddTemplate.Content = m_modelName;
            m_gameCreateCallback = newGameCallback;
            m_templateCreateCallback = newTemplateCallback;
            FillTemplateList(userList);
            ValidateSelection();
        }

        private string InitializeNewGameButtonText(string text)
        {
            return string.Format(btnAddTemplate.Content.ToString(), text);
        }
        private void FillTemplateList(CCBGameTemplateList userTemplates)
        {
            List<CCBStockTemplate> stockTemplates = CCBStockTemplates.StockTemplateList;

            lbTemplates.Items.Clear();
            foreach (CCBStockTemplate template in stockTemplates)
                lbTemplates.Items.Add(new GameTemplateEntry(template.Name, template.Template));
            if (null != userTemplates)
                foreach (CCBGameTemplate userTemplate in userTemplates)
                    lbTemplates.Items.Add(new GameTemplateEntry(userTemplate.Name, userTemplate));
        }
        private void AddTemplateToList(CCBGameTemplate template)
        {
            lbTemplates.Items.Add(new GameTemplateEntry(template.Name, template));
        }
        private void AddTemplateToList(GameTemplateEntry entry)
        {
            lbTemplates.Items.Add(entry);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnAddGame_Click(object sender, RoutedEventArgs e)
        {
            GameTemplateEntry entry = (GameTemplateEntry)lbTemplates.SelectedItem;

            m_gameCreateCallback(entry.Template, tbName.Text);
            this.Close();
        }
        private void btnAddTemplate_Click(object sender, RoutedEventArgs e)
        {
            CCBGameTemplate newTemplate = m_templateCreateCallback(m_model, tbName.Text);
            GameTemplateEntry entry = new GameTemplateEntry(tbName.Text, newTemplate);

            AddTemplateToList(entry);
        }

        private void lbTemplates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateSelection();
        }
        private void ValidateSelection()
        {
            if (-1 == lbTemplates.SelectedIndex)
                btnAddGame.IsEnabled = false;
            else
            {
                btnAddGame.IsEnabled = true;
                tbName.Text = lbTemplates.SelectedItem.ToString();
            }
        }

    }
}
