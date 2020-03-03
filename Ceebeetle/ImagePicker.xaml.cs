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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ceebeetle
{
    /// <summary>
    /// Interaction logic for ImagePicker.xaml
    /// </summary>
    public partial class ImagePicker : CCBChildWindow
    {
        string m_imagePath;
        public string ImagePath
        {
            get { return m_imagePath; }
        }

        public ImagePicker()
        {
            m_imagePath = null;
            InitializeComponent();
            CeebeetleWindowInit();
            btnSelect.IsEnabled = false;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            m_imagePath = tbImageSrc.Text;
            Close();
        }

        private string GetSelectedImage()
        {
            if (-1 != lbImages.SelectedIndex)
            {
                Image imgSel = (Image)lbImages.Items[lbImages.SelectedIndex];

                return imgSel.Source.ToString();
            }
            return null;
        }
        private void lbImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string strImgSrc = GetSelectedImage();

            if (null != strImgSrc)
            {
                tbImageSrc.Text = strImgSrc;
                btnSelect.IsEnabled = true;
            }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (BrowseForFile(tbImageSrc))
                btnSelect.IsEnabled = true;
        }

        private void CCBChildWindow_Drop(object sender, DragEventArgs e)
        {
            Log("Drop happened.");
            if (e.AllowedEffects.HasFlag(DragDropEffects.Link))
            {
                try
                {
                    string[] formats = e.Data.GetFormats();

                    if (formats.Contains("FileName"))
                    {
                        string[] filenames = (string[])e.Data.GetData("FileName");

                        if ((null != filenames) && (0 < filenames.Length))
                        {
                            tbImageSrc.Text = filenames[0];
                            e.Effects = DragDropEffects.Link;
                            lbImages.SelectedIndex = -1;
                            btnSelect.IsEnabled = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("Exception in image drop: " + ex.Message);
                }
            }
        }

        private void tbImageSrc_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (0 < tbImageSrc.Text.Length)
                btnSelect.IsEnabled = true;
        }
    }
}
