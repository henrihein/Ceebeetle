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

        private void Test()
        {
            try
            {
                BitmapImage img2 = ControlHelpers.NewImage(@"C:\work\Games\Images\Demi4.png");
                BitmapImage img1 = ControlHelpers.NewImage("/Ceebeetle;component/Resources/BasicAdventurer-Beater.png");

                Log("Created 2 images.");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        public CharacterSheetWnd(CCBCharacter character) : base()
        {
            m_character = character;
            InitializeComponent();
            InitMinSize();
            if (null != character)
                Title = string.Format(Title, character.Name);
            Test();
            PopulateSheet();
        }

        public void PopulateSheet()
        {
            try
            {
                int statLen = m_character.PropertyList.GetLongestNameLen(12);
                StringBuilder strStatText = new StringBuilder();
                string strStatLineFmt = "{0:-1}  {1:" + string.Format("{0}", statLen) + "}\n";

                elCharacterTitle.Inlines.Add(new Bold(new Run(m_character.Name)));
                if ((null != m_character.Image) && (0 < m_character.Image.Length))
                {
                    elCharacterImage.Source = ControlHelpers.NewImage(m_character.Image);
                }
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
    }
}
