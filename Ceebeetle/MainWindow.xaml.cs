using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Timers;

namespace Ceebeetle
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CCBConfig m_config;
        CCBCharacterList m_characters;
        NewCharacter m_characterWnd;
        private bool m_deleteEnabled;
        BackgroundWorker m_worker;
        DoWorkEventHandler m_loadCompletedD;
        Timer m_timer;
        private delegate void DOnCharacterListUpdate(TStatusUpdate[] args);
        DOnCharacterListUpdate m_onCharacterListUpdateD;

        public MainWindow()
        {
            m_config = new CCBConfig();
            m_characters = new CCBCharacterList();
            m_deleteEnabled = false;
            m_onCharacterListUpdateD = new DOnCharacterListUpdate(OnCharacterListUpdate);
            m_characterWnd = new NewCharacter(new OnNewCharacter(AddCharacter));
            m_worker = new BackgroundWorker();
            m_timer = new Timer(11777); //133337);
            m_timer.Elapsed += new ElapsedEventHandler(OnTimer);
            m_timer.Start();

            InitializeComponent();
            try
            {
                m_config.Initialize();
                tbStatus.Text = System.String.Format("{0} [v{1}]", m_config.DocPath, System.Environment.Version.ToString());
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                System.Diagnostics.Debug.Write("Error caught in Main.");
                System.Diagnostics.Debug.Write(ex.ToString());
            }
            m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_OnPersistenceCompleted);
            m_loadCompletedD = new DoWorkEventHandler(m_characters.LoadCharacters);
            m_worker.DoWork += m_loadCompletedD;
            m_worker.RunWorkerAsync(m_config.DocPath);
        }

        
        void MainWindow_Closing(object sender, CancelEventArgs evtArgs)
        {
            m_timer.Stop();
            m_timer.Close();
            m_characterWnd.SetShutdown();
            m_characterWnd.Close();
        }
        private void btnAddCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (m_characterWnd.IsVisible)
                m_characterWnd.Activate();
            else
                m_characterWnd.Show();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            m_characterWnd.Close();
            this.Close();
        }

        public void AddCharacter(CCBCharacter newCharacter)
        {
            m_characters.AddSafe(newCharacter);
            MergeCharacterList();
        }
        public void MergeCharacterList()
        {
            foreach (CCBCharacter character in m_characters)
            {
                if (!lbCharacters.Items.Contains(character))
                    lbCharacters.Items.Add(character);
            }
        }
        private void OnCharacterListUpdate(TStatusUpdate[] args)
        {
            if (0 != args.Rank)
            {
                switch (args[0])
                {
                    case TStatusUpdate.tsuCancelled:
                        tbLastError.Text = "Something was Canceled";
                        break;
                    case TStatusUpdate.tsuError:
                        tbLastError.Text = "Error in saving or loading file:"; // + evtArgs.Error.ToString();
                        break;
                    default:
                        tbLastError.Text = "File has been loaded";
                        MergeCharacterList();
                        break;
                }
            }
        }
        private void Worker_OnPersistenceCompleted(object sender, RunWorkerCompletedEventArgs evtArgs)
        {
            TStatusUpdate tsu = TStatusUpdate.tsuNone;

            //TODO: Is this the right way to stop the background loader?
            m_worker.DoWork -= m_loadCompletedD;
            if (evtArgs.Cancelled)
                tsu = TStatusUpdate.tsuCancelled;
            else if (null != evtArgs.Error)
                tsu = TStatusUpdate.tsuError;
            else
                tsu = TStatusUpdate.tsuFileSaved;
            TStatusUpdate[] args = new TStatusUpdate[1]{tsu};

            if (Application.Current.Dispatcher.CheckAccess())
            {
                OnCharacterListUpdate(args);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(m_onCharacterListUpdateD, args);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (m_deleteEnabled)
            {
                List<object> selected = new List<object>();

                foreach (object oItem in lbCharacters.SelectedItems)
                    selected.Add(oItem);
                foreach (object oItem in selected)
                    lbCharacters.Items.Remove(oItem);
                m_characters.DeleteSafe(selected);
            }
            else
            {
                m_deleteEnabled = true;
                btnDelete.Content = "Delete Selected";
            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(m_characters.AsXML());
        }
        private void OnTimer(object source, ElapsedEventArgs evtArgs)
        {
            if (m_characters.IsDirty())
            {
                m_worker.DoWork += new DoWorkEventHandler(m_characters.SaveCharacters);
                if (!m_worker.IsBusy)
                    m_worker.RunWorkerAsync(m_config.DocPath);
            }
        }
    }
}
