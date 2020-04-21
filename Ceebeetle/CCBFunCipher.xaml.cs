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
    /// Interaction logic for CCBFunCipher.xaml
    /// </summary>
    public partial class CCBFunCipher : CCBChildWindow
    {
        static int m_saltSize = 31;
        const string m_lookup = "MnHWO7Izjr$g1Dxp5EFlYumTPSe2ak64qLoBsfcXt9iC3R#NvdQwAG0yJbZKh8UV";
        string m_keyphrase;
        string m_plainText = "The quick brown fox jumps over the lazy dog.";
        string m_cipher;
        int[] m_salt;
        bool m_dirty;

        enum CipherViewMode
        {
            cvm_none = 0,
            cvm_keyphrase,
            cvm_cipher,
            cvm_plaintext,
            cvm_lookup,
            cvm_help
        };
        CipherViewMode m_cvm;

        public CCBFunCipher()
        {
            m_salt = new int[m_saltSize];
            m_cipher = null;
            m_dirty = false;
            InitializeComponent();
            CeebeetleWindowInit();
            HideCtl(helpDoc);
            tbData.Text = m_plainText;
            m_keyphrase = "CaesarAndBellaso";
            SetView(CipherViewMode.cvm_plaintext);
        }

        private void SetView(CipherViewMode cvm)
        {
            m_cvm = cvm;
            switch (m_cvm)
            {
                case CipherViewMode.cvm_keyphrase:
                    lView.Content = "Key phrase";
                    break;
                case CipherViewMode.cvm_plaintext:
                    lView.Content = "Plain text";
                    break;
                case CipherViewMode.cvm_cipher:
                    lView.Content = "Cipher text";
                    break;
                case CipherViewMode.cvm_lookup:
                    lView.Content = "Lookup Map";
                    break;
                case CipherViewMode.cvm_help:
                    lView.Content = "Help";
                    break;
                default:
                    break;
            }
        }
        private void MaybeSave()
        {
            if (m_dirty)
            {
                switch (m_cvm)
                {
                    case CipherViewMode.cvm_keyphrase:
                        m_keyphrase = tbData.Text;
                        break;
                    case CipherViewMode.cvm_plaintext:
                        m_plainText = tbData.Text;
                        m_cipher = null;
                        break;
                    case CipherViewMode.cvm_cipher:
                        m_cipher = tbData.Text;
                        m_plainText = null;
                        break;
                    default:
                        break;
                }
            }
        }
        private void ToWork()
        {
            MaybeSave();
            ShowCtl(tbData);
            HideCtl(helpDoc);
        }
        private void ToHelp()
        {
            ShowCtl(helpDoc);
            HideCtl(tbData);
            SetView(CipherViewMode.cvm_help);
        }
        private bool EncodeDef()
        {
            if (m_dirty || (null == m_cipher))
            {
                m_cipher = Encode(m_plainText);
                return true;
            }
            return false;
        }
        private string Encode(string text)
        {
            string textData = text.ToUpper();
            char[] plainText = textData.ToArray<char>();
            char[] cipherText = new char[plainText.Length];
            int ixCipher = 0, ixKey = 0;
            string key = SanitizeKey(m_keyphrase);

            for (int ix = 0; ix < plainText.Length; ix++)
            {
                if ('\n' == plainText[ix])
                    cipherText[ixCipher++] = '\n';
                else if ((' ' <= plainText[ix]) && (m_lookup.Length > (plainText[ix] - ' ')))
                {
                    int ixLookup = (plainText[ix] - ' ') + (key[ixKey++] - ' ');

                    cipherText[ixCipher++] = m_lookup[ixLookup % m_lookup.Length];
                    if (key.Length <= ixKey)
                        ixKey = 0;
                }
            }
            //In case we skipped un-encodeable characters:
            for (int ixTail = ixCipher + 1; ixTail < cipherText.Length; ixTail++ )
                cipherText[ixTail] = '\0';
            return new string(cipherText);
        }
        private bool DecodeDef()
        {
            if (m_dirty || (null == m_plainText))
            {
                m_plainText = Decode(m_cipher);
                return true;
            }
            return false;
        }
        private string Decode(string cipher)
        {
            char[] cipherText = cipher.ToArray<char>();
            char[] plainData = new char[cipherText.Length];
            char[] reverseLookup = new char[255];
            int ixKey = 0;
            string key = SanitizeKey(m_keyphrase);

            //First construct the reverse lookup map
            for (int ixLookup = 0; ixLookup < m_lookup.Length; ixLookup++)
            {
                reverseLookup[m_lookup[ixLookup]] = (char)ixLookup;
            }
            for (int ix = 0; ix < cipherText.Length; ix++)
            {
                if ('\n' == cipherText[ix])
                    plainData[ix] = '\n';
                else if (cipherText[ix] < reverseLookup.Length)
                {
                    int chKey = key[ixKey++] - ' ';
                    int ixPlain = reverseLookup[cipherText[ix]];

                    if (0 == ixPlain)   //Unknown character in cipher
                        break;
                    if (ixPlain >= chKey)
                        ixPlain -= chKey;
                    else
                        ixPlain += m_lookup.Length - chKey;
                    plainData[ix] = (char)(' ' + ixPlain);
                    if (key.Length <= ixKey)
                        ixKey = 0;
                }
            }
            return new string(plainData);
        }
        private string SanitizeKey(string keyData)
        {
            string strKey = keyData.ToUpper();
            char[] newKey = new char[strKey.Length];

            for (int ix = 0; ix < strKey.Length; ix++)
            {
                if (strKey[ix] < (' ' + m_lookup.Length))
                    newKey[ix] = strKey[ix];
                else
                    newKey[ix] = (char)(' ' + strKey[ix] % m_lookup.Length);
            }
            return new string(newKey);
        }
        private void OnObjectClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Hyperlink hl = (Hyperlink)sender;
                string link = hl.NavigateUri.ToString();

                System.Diagnostics.Process.Start(link);
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (Exception ex)
            {
                CCBLogConfig.GetLogger().Log("OnObjectClicked: " + ex.Message);
            }
        }
        private void btnMap_Click(object sender, RoutedEventArgs e)
        {
            ToWork();
            tbData.Text = m_lookup;
            tbData.IsReadOnly = true;
            lView.Content = "Lookup Map";
            SetView(CipherViewMode.cvm_lookup);
        }

        private void btnPlain_Click(object sender, RoutedEventArgs e)
        {
            ToWork();
            DecodeDef();
            tbData.Text = m_plainText;
            tbData.IsReadOnly = false;
            m_dirty = false;
            lView.Content = "Lookup Map";
            SetView(CipherViewMode.cvm_plaintext);
        }

        private void btnCipher_Click(object sender, RoutedEventArgs e)
        {
            ToWork();
            EncodeDef();
            tbData.Text = m_cipher;
            tbData.IsReadOnly = false;
            m_dirty = false;
            SetView(CipherViewMode.cvm_cipher);
        }
        private void btnKey_Click(object sender, RoutedEventArgs e)
        {
            ToWork();
            tbData.Text = m_keyphrase;
            m_dirty = false;
            SetView(CipherViewMode.cvm_keyphrase);
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            ToHelp();
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void tbData_TextChanged(object sender, TextChangedEventArgs e)
        {
            m_dirty = true;
        }

    }
}
