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
    /// Interaction logic for CharacterSheetWnd.xaml
    /// </summary>
    public partial class CharacterSheetWnd : CCBChildWindow
    {
        CCBCharacter m_character;

        public CharacterSheetWnd(CCBCharacter character) : base()
        {
            m_character = character;
            InitializeComponent();
            InitMinSize();
            if (null != character)
                Title = string.Format(Title, character.Name);
            PopulateSheet();
        }
        private void UpdateCharacterImage()
        {
            if ((null != m_character.Image) && (0 < m_character.Image.Length))
            {
                elCharacterImage.Source = ControlHelpers.NewImage(m_character.Image);
            }
        }
        public void PopulateSheet()
        {
            try
            {
                int statLen = m_character.PropertyList.GetLongestNameLen(12);
                StringBuilder strStatText = new StringBuilder();
                string strStatLineFmt = "{0:-1}  {1:" + string.Format("{0}", statLen) + "}\n";

                UpdateCharacterImage();
                elCharacterTitle.Inlines.Add(new Bold(new Run(m_character.Name)));
                elCharacterStats.Inlines.Clear();
                foreach (CCBCharacterProperty charProp in m_character.PropertyList)
                {
                    strStatText.AppendFormat(strStatLineFmt, charProp.Name, charProp.Value);
                }
                elCharacterStats.Inlines.Add(new Run(strStatText.ToString()));
                elCharacterItems.Inlines.Add(new Run(m_character.Items.RenderString()));
                foreach (CCBBag bag in m_character.BagList)
                {
                    elCharacterItems.Inlines.Add(new Run(bag.RenderString()));
                }
            }
            catch (Exception ex)
            {
                Log("Error in PopulateSheet: {0}", ex.Message);
            }
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnAddImage_Click(object sender, RoutedEventArgs e)
        {
            ImagePicker imgPicker = new ImagePicker();

            imgPicker.ShowDialog(this);
            if (true == imgPicker.DialogResult)
            {
                m_character.Image = imgPicker.ImagePath;
                UpdateCharacterImage();
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDlg = new PrintDialog();

            if (true == printDlg.ShowDialog())
            {
                printDlg.PrintDocument(docSheetViewer.Document.DocumentPaginator, m_character.Name);
            }
        }
    }
}
