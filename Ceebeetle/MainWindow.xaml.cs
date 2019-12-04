﻿using System;
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
using System.IO;    //For IOException

namespace Ceebeetle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool m_deleteEnabled;
        CCBConfig m_config;
        CCBGames m_games;
        BackgroundWorker m_worker;
        DoWorkEventHandler m_loaderD;
        Timer m_timer;
        private delegate void DOnCharacterListUpdate(TStatusUpdate[] args);
        DOnCharacterListUpdate m_onCharacterListUpdateD;
        CCBTreeViewGameAdder m_gameAdderEntry;

        public MainWindow()
        {
            m_config = new CCBConfig();
            m_games = new CCBGames();
            m_deleteEnabled = false;
            m_onCharacterListUpdateD = new DOnCharacterListUpdate(OnCharacterListUpdate);
            m_gameAdderEntry = new CCBTreeViewGameAdder();
            m_worker = new BackgroundWorker();
            m_worker.WorkerReportsProgress = true;
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
            m_worker.ProgressChanged += new ProgressChangedEventHandler(Worker_OnProgressChanged);
            m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_OnPersistenceCompleted);
            m_loaderD = new DoWorkEventHandler(m_games.LoadGames);
            m_worker.DoWork += m_loaderD;
            m_worker.RunWorkerAsync(m_config.DocPath);
            AddOrMoveAdder();
        }

        
        void MainWindow_Closing(object sender, CancelEventArgs evtArgs)
        {
            m_timer.Stop();
            m_timer.Close();
            try
            {
                //Save here always, in case there was some problem with the dirty logic.
                m_games.SaveGames(m_config.DocPath);
            }
            catch (IOException iox)
            {
                System.Diagnostics.Debug.Write(iox.ToString());
                evtArgs.Cancel = true;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddOrMoveAdder()
        {
            tvGames.Items.Remove(m_gameAdderEntry);
            tvGames.Items.Add(m_gameAdderEntry);
        }
        public void MergeCharacterList()
        {
            tvGames.Items.Clear();
            foreach (CCBGame game in m_games)
            {
                CCBTreeViewGame newGameItem = new CCBTreeViewGame(game);
                int ix = tvGames.Items.Add(newGameItem);

                newGameItem.StartBulkEdit();
                foreach (CCBCharacter character in game.Characters)
                    newGameItem.Add(character);
                newGameItem.EndBulkEdit();
            }
            AddOrMoveAdder();
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
                    case TStatusUpdate.tsuFileSaved:
                        tbLastError.Text = "File saved.";
                        break;
                    case TStatusUpdate.tsuFileLoaded:
                        tbLastError.Text = "File loaded";
                        MergeCharacterList();
                        btnDelete.IsEnabled = true;
                        tvGames.IsEnabled = true;
                        btnAddGame.IsEnabled = true;
                        btnSave.IsEnabled = true;
                        break;
                    case TStatusUpdate.tsuFileNothingLoaded:
                        tbLastError.Text = "No file to load";
                        btnDelete.IsEnabled = true;
                        tvGames.IsEnabled = true;
                        btnAddGame.IsEnabled = true;
                        btnSave.IsEnabled = true;
                        break;
                    default:
                        tbLastError.Text = "Unknown tsu in persistence event.";
                        break;
                }
            }
        }
        private void Worker_OnProgressChanged(object sender, ProgressChangedEventArgs evtArgs)
        {
            //Overloading the progress mechanism to remove the loader task.
            if (1 == evtArgs.ProgressPercentage)
                m_worker.DoWork -= m_loaderD;
        }
        private void Worker_OnPersistenceCompleted(object sender, RunWorkerCompletedEventArgs evtArgs)
        {
            TStatusUpdate tsu = TStatusUpdate.tsuNone;

            if (evtArgs.Cancelled)
                tsu = TStatusUpdate.tsuCancelled;
            else if (null != evtArgs.Error)
                tsu = TStatusUpdate.tsuError;
            else if (null != evtArgs.Result)
                tsu = (TStatusUpdate)evtArgs.Result;
            TStatusUpdate[] args = new TStatusUpdate[1] { tsu };

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
                CCBTreeViewItem selItem = (CCBTreeViewItem)tvGames.SelectedItem;

                if (null == selItem)
                    tbLastError.Text = "Wrong item in treeview:";
                else
                {
                    switch (selItem.ItemType)
                    {
                        case CCBItemType.itpCharacter:
                        {
                            CCBCharacter character = selItem.Character;

                            if (null == character)
                                tbLastError.Text = String.Format("Mismatch in CBTVI selected ({0})", selItem.ItemType);
                            else
                            {
                                CCBTreeViewGame gameNode = FindGameFromNode(selItem);

                                if (null == gameNode)
                                    tbLastError.Text = "Internal error: cannot find game node.";
                                else
                                {
                                    CCBGame game = gameNode.Game;

                                    gameNode.Items.Remove(selItem);
                                    game.DeleteCharacter(character);
                                }
                            }
                            break;
                        }
                        case CCBItemType.itpGame:
                        {
                            CCBGame game = selItem.Game;

                            if (null == game)
                                tbLastError.Text = String.Format("Mismatch in CBTVI selected ({0})", selItem.ItemType);
                            else
                            {
                                tvGames.Items.Remove(selItem);
                                m_games.DeleteGameSafe(game);
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                m_deleteEnabled = true;
                btnDelete.Content = "Delete Selected";
            }
        }

        private void OnTimer(object source, ElapsedEventArgs evtArgs)
        {
            if (m_games.IsDirty)
            {
                m_worker.DoWork += new DoWorkEventHandler(m_games.SaveGames);
                if (!m_worker.IsBusy)
                    m_worker.RunWorkerAsync(m_config.DocPath);
            }
        }

        private CCBTreeViewGame FindCurrentGame()
        {
            return FindGameFromNode((TreeViewItem)tvGames.SelectedItem);
        }
        private CCBTreeViewGame FindGameFromNode(TreeViewItem node)
        {
            while (null != node)
            {
                if (node is CCBTreeViewGame)
                {
                    CCBTreeViewGame game = (CCBTreeViewGame)node;

                    if (null != game)
                        return game;
                }
                node = (TreeViewItem)node.Parent;
            }
            return null;
        }
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(m_games.AsXML());
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            CEditMode editMode = CEditModeProperty.GetEditNode(tbItem);

            if (null == editMode) return;
            if (EEditMode.em_Frozen == editMode.EditMode) return;
            switch (editMode.EditMode)
            {
                case EEditMode.em_None:
                    tbLastError.Text = "No mode.";
                    break;
                case EEditMode.em_AddCharacter:
                {
                    CCBTreeViewGame currentGameNode = FindGameFromNode(editMode.Node);

                    if ((null == currentGameNode) || (CCBItemType.itpGame != currentGameNode.ItemType))
                        tbLastError.Text = "No game selected.";
                    else
                    {
                        CCBCharacter newCharacter = new CCBCharacter(tbItem.Text);
                        CCBTreeViewItem characterNode = currentGameNode.Add(newCharacter);

                        currentGameNode.Game.AddCharacter(newCharacter);
                    }
                    break;
                }
                case EEditMode.em_AddGame:
                    CCBGame newGame = m_games.AddGame(tbItem.Text);

                    tvGames.Items.Add(new CCBTreeViewGame(newGame));
                    AddOrMoveAdder();
                    break;
                case EEditMode.em_ModifyCharacter:
                    if (null == editMode.Node)
                    {
                        tbLastError.Text = "Internal error: No edit node.";
                        return;
                    }
                    editMode.Node.Header = tbItem.Text;
                    editMode.Node.Character.Name = tbItem.Text;
                    break;
                case EEditMode.em_ModifyGame:
                {
                    CCBTreeViewGame currentGameNode = FindGameFromNode(editMode.Node);

                    currentGameNode.Game.Name = tbItem.Text;
                    currentGameNode.Header = tbItem.Text;
                    break;
                }
                default:
                    tbLastError.Text = "Unknown mode.";
                    break;
            }
        }

        private EEditMode AddCharacterView()
        {
            gbItemView.Header = "Add Character";
            btnSave.Content = "Add";
            tbItem.Text = "New Hero";
            return EEditMode.em_AddCharacter;
        }
        private EEditMode AddGameView()
        {
            gbItemView.Header = "Add Game";
            btnSave.Content = "Add";
            tbItem.Text = "New Game";
            return EEditMode.em_AddGame;
        }
        private EEditMode ModifyCharacterView(CCBCharacter character)
        {
            gbItemView.Header = "Modify Character";
            btnSave.Content = "Save";
            if (null != character)
                tbItem.Text = character.Name;
            else
                tbItem.Text = "";
            return EEditMode.em_ModifyCharacter;
        }
        private EEditMode ModifyGameView(CCBGame game)
        {
            gbItemView.Header = "Modify Game";
            btnSave.Content = "Save";
            if (null != game)
                tbItem.Text = game.Name;
            else
                tbItem.Text = "";
            return EEditMode.em_ModifyGame;
        }
        private void OnItemSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            CCBTreeViewItem selItem = (CCBTreeViewItem)tvGames.SelectedItem;

            if (null == selItem)
            {
                tbLastError.Text = "Wrong item in treeview:";
                btnSave.IsEnabled = false;
            }
            else
            {
                CEditMode em = new CEditMode(selItem);

                btnSave.IsEnabled = true;
                switch (selItem.ItemType)
                {
                    case CCBItemType.itpCharacter:
                        em.EditMode = ModifyCharacterView(selItem.Character);
                        btnDelete.IsEnabled = true;
                        break;
                    case CCBItemType.itpGame:
                        em.EditMode = ModifyGameView(selItem.Game);
                        btnDelete.IsEnabled = true;
                        break;
                    case CCBItemType.itpGameAdder:
                        em.EditMode = AddGameView();
                        btnDelete.IsEnabled = false;
                        break;
                    case CCBItemType.itpCharacterAdder:
                        em.EditMode = AddCharacterView();
                        btnDelete.IsEnabled = false;
                        break;
                }
                tbItem.SelectAll();
                CEditModeProperty.SetEditNode(tbItem, em);
                //tbItem.Focus();
            }
        }

    }
}
