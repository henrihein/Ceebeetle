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
        CCBGameData m_games;
        List<CCBGameTemplate> m_templates;
        BackgroundWorker m_worker;
        DoWorkEventHandler m_loaderD;
        Timer m_timer;
        private delegate void DOnCharacterListUpdate(TStatusUpdate[] args);
        private delegate void DOnAddingNewEntityMode();
        DOnCharacterListUpdate m_onCharacterListUpdateD;
        DOnAddingNewEntityMode m_onAddingNewEntityModeD;
        DOnCreateNewGame m_onCreateNewGameD;
        DOnCreateNewTemplate m_onCreateNewTemplateD;
        CCBTreeViewGameAdder m_gameAdderEntry;

        public MainWindow()
        {
            m_config = new CCBConfig();
            m_games = new CCBGameData();
            m_templates = new List<CCBGameTemplate>();
            m_deleteEnabled = false;
            m_onCharacterListUpdateD = new DOnCharacterListUpdate(OnCharacterListUpdate);
            m_onAddingNewEntityModeD = new DOnAddingNewEntityMode(OnAddingNewEntityMode);
            m_onCreateNewGameD = new DOnCreateNewGame(OnCreateNewGame);
            m_onCreateNewTemplateD = new DOnCreateNewTemplate(OnCreateNewTemplate);
            m_gameAdderEntry = new CCBTreeViewGameAdder();
            m_worker = new BackgroundWorker();
            m_worker.WorkerReportsProgress = true;
            m_timer = new Timer(133337);
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
            m_worker.RunWorkerAsync(m_config);
            SetDefaultView();
            AddOrMoveAdder();
        }

        
        void MainWindow_Closing(object sender, CancelEventArgs evtArgs)
        {
            m_timer.Stop();
            m_timer.Close();
            try
            {
                //Save here always, in case there was some problem with the dirty logic.
                if (!m_games.SaveGames(m_config))
                {
                    MessageBoxResult mbr = System.Windows.MessageBox.Show("There was an error saving games. Do you still want to exit?", "Confirm Ceebeetle Exit", MessageBoxButton.YesNo);

                    if (mbr == MessageBoxResult.No)
                        evtArgs.Cancel = true;
                }
            }
            catch (IOException iox)
            {
                System.Diagnostics.Debug.Write(iox.ToString());
                evtArgs.Cancel = true;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.CloseAll();
            this.Close();
        }

        private void AddOrMoveAdder()
        {
            tvGames.Items.Remove(m_gameAdderEntry);
            tvGames.Items.Add(m_gameAdderEntry);
        }

        #region Callbacks
        public void LoadCharacterList()
        {
            tvGames.Items.Clear();
            foreach (CCBGame game in m_games)
            {
                CCBTreeViewGame newGameItem = new CCBTreeViewGame(game);
                int ix = tvGames.Items.Add(newGameItem);

                newGameItem.StartBulkEdit();
                foreach (CCBCharacter character in game.Characters)
                {
                    CCBTreeViewCharacter newCharacterNode = newGameItem.Add(character);

                    newCharacterNode.StartBulkEdit();
                    AddPropertiesToCharacterNode(newCharacterNode);
                    AddBagsToCharacterNode(newCharacterNode);

                    newCharacterNode.EndBulkEdit();
                }
                AddBagsToGameNode(newGameItem);
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
                    case TStatusUpdate.tsuParseError:
                        tbLastError.Text = "Error in saving or loading file:"; // + evtArgs.Error.ToString();
                        break;
                    case TStatusUpdate.tsuFileSaved:
                        tbLastError.Text = "File saved.";
                        break;
                    case TStatusUpdate.tsuFileLoaded:
                        tbLastError.Text = "File loaded";
                        LoadCharacterList();
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
        private void OnTimer(object source, ElapsedEventArgs evtArgs)
        {
            if (m_games.IsDirty)
            {
                m_worker.DoWork += new DoWorkEventHandler(m_games.SaveGames);
                if (!m_worker.IsBusy)
                    m_worker.RunWorkerAsync(m_config);
            }
        }
        private void OnAddingNewEntityMode()
        {
            //Still cannot set focus here, as it deselects child nodes in the treeview control.
            //TODO, sigh.
            //tbItem.Focus();
            return;
        }
        private void OnCreateNewGame(CCBGameTemplate template, string name)
        {
            CCBGame newGame = m_games.AddGame(name, template);
            CCBTreeViewGame gameNode = new CCBTreeViewGame(newGame);

            tvGames.Items.Add(gameNode);
            AddBagsToGameNode(gameNode);
            AddOrMoveAdder();
        }
        private CCBGameTemplate OnCreateNewTemplate(CCBGame gameFrom, string name)
        {
            CCBGameTemplate newTemplate = m_games.AddTemplate(name, gameFrom);

            return newTemplate;
        }
        private void OnIsCountableChecked(object sender, RoutedEventArgs e)
        {
            tbValue.IsEnabled = (true == cbCountable.IsChecked);
        }
        public void OnCopyBagItems(CCBBag targetBag, string[] bagItems)
        {
            CCBTreeViewBag bagNode = FindBagNodeFromBag(targetBag);

            if (null != bagNode)
            {
                CCBBag bag = bagNode.Bag;

                bagNode.StartBulkEdit();
                foreach (string item in bagItems)
                {
                    CCBBagItem bagItem = bag.AddItem(item);
                    bagNode.Items.Add(new CCBTreeViewItem(bagItem));
                }
                bagNode.EndBulkEdit();
            }
        }
        public bool OnDeleteBagItems(CCBBag targetBag, string[] bagItems)
        {
            CCBTreeViewBag bagNode = FindBagNodeFromBag(targetBag);

            if (null != bagNode)
            {
                CCBBag bag = bagNode.Bag;

                bagNode.StartBulkEdit();
                foreach (string item in bagItems)
                {
                    bag.RemoveItem(item);
                    bagNode.Remove(item);
                }
                bagNode.EndBulkEdit();
                ResetEntitiesList();
                return true;
            }
            return false;
        }
        public void OnCopyName(string name)
        {
            tbItem.Text = name;
        }
        #endregion //Callbacks

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
                        case CCBItemType.itpProperty:
                        {
                            CCBCharacterProperty property = selItem.Property;

                            if (null == property)
                                tbLastError.Text = String.Format("Mismatch in CBTVI selected ({0})", selItem.ItemType);
                            else
                            {
                                CCBTreeViewCharacter characterNode = FindCharacterFromNode(selItem);
                                CCBTreeViewGame gameNode = FindGameFromNode(selItem);

                                if (null != characterNode)
                                {
                                    characterNode.Items.Remove(selItem);
                                    characterNode.Character.RemovePropertySafe(property);
                                    if (null != gameNode)
                                    {
                                        CCBGame game = gameNode.Game;

                                        game.CheckPropertyForDeletion(property.Name);
                                    }
                                }
                            }
                            break;
                        }
                        case CCBItemType.itpBag:
                        {
                            CCBBag bag = selItem.Bag;

                            if (null == bag)
                                tbLastError.Text = String.Format("Mismatch in CBTVI selected ({0})", selItem.ItemType);
                            else
                            {
                                CCBTreeViewCharacter characterNode = FindCharacterFromNode(selItem);

                                if (null != characterNode)
                                    characterNode.Character.RemoveBag(bag);
                                characterNode.Items.Remove(selItem);
                            }
                            break;
                        }
                        case CCBItemType.itpBagItem:
                        {
                            CCBBagItem bagItem = selItem.BagItem;

                            if (null == bagItem)
                                tbLastError.Text = String.Format("Mismatch in CBTVI selected ({0})", selItem.ItemType);
                            else
                            {
                                CCBTreeViewBag bagNode = FindBagFromNode(selItem);

                                if (null != bagNode)
                                    bagNode.Bag.RemoveItem(bagItem);
                                bagNode.Items.Remove(selItem);
                            }
                            break;
                        }
                        default:
                            break;
                    }
                }
            }
            else
            {
                m_deleteEnabled = true;
                btnDelete.Content = "Delete Selected";
            }
        }

        #region SelectionViewHelpers
        private CCBTreeViewItem GetSelectedNode()
        {
            return (CCBTreeViewItem)tvGames.SelectedItem;
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
        private CCBTreeViewCharacter FindCharacterFromNode(TreeViewItem node)
        {
            while (null != node)
            {
                if (node is CCBTreeViewCharacter)
                {
                    CCBTreeViewCharacter character = (CCBTreeViewCharacter)node;

                    if (null != character)
                        return character;
                }
                node = (TreeViewItem)node.Parent;
            }
            return null;
        }
        private CCBTreeViewBag FindBagFromNode(TreeViewItem node)
        {
            while (null != node)
            {
                if (node is CCBTreeViewBag)
                {
                    CCBTreeViewBag bag = (CCBTreeViewBag)node;

                    if (null != bag)
                        return bag;
                }
                node = (TreeViewItem)node.Parent;
            }
            return null;
        }
        private CCBTreeViewBag FindBagNodeFromBag(CCBBag bag)
        {
            foreach (TreeViewItem gameNode in tvGames.Items)
            {
                foreach (TreeViewItem subNode in gameNode.Items)
                {
                    if (subNode.GetType() == typeof(CCBTreeViewBag))
                    {
                        CCBTreeViewBag bagNode = (CCBTreeViewBag)subNode;

                        if (ReferenceEquals(bagNode.Bag, bag))
                            return bagNode;
                    }
                    if (subNode.GetType() == typeof(CCBTreeViewCharacter))
                    {
                        foreach (TreeViewItem characterSubNode in subNode.Items)
                        {
                            if (characterSubNode.GetType() == typeof(CCBTreeViewBag))
                            {
                                CCBTreeViewBag bagNode = (CCBTreeViewBag)characterSubNode;

                                if (ReferenceEquals(bagNode.Bag, bag))
                                    return bagNode;
                            }
                        }
                    }
                }
            }
            return null;
        }
        private void AddBagToNode(CCBTreeViewItem node, CCBBag bag)
        {
            if (null != bag)
            {
                CCBTreeViewBag bagNode;

                node.StartBulkEdit();
                bagNode = node.Add(bag);
                foreach (CCBBagItem bagItem in bag.Items)
                    bagNode.Add(bagItem);
                node.EndBulkEdit();
            }
        }
        private void AddBagToCharacterNode(CCBTreeViewCharacter characterNode, CCBBag bag)
        {
            AddBagToNode(characterNode, bag);
        }
        private void AddBagsToCharacterNode(CCBTreeViewCharacter characterNode)
        {
            CCBCharacter character = characterNode.Character;

            AddBagToCharacterNode(characterNode, character.Items);
            if (null != character.BagList)
            {
                foreach (CCBBag bag in character.BagList)
                    AddBagToCharacterNode(characterNode, bag);
            }
        }
        private void AddPropertiesToCharacterNode(CCBTreeViewCharacter characterNode)
        {
            CCBCharacter character = characterNode.Character;

            System.Diagnostics.Debug.Assert(null != character);
            System.Diagnostics.Debug.Assert(null != character.PropertyList);
            if (null != character.PropertyList)
            {
                foreach (CCBCharacterProperty property in character.PropertyList)
                    characterNode.Add(property);
            }
        }
        private void AddBagsToGameNode(CCBTreeViewGame gameNode)
        {
            CCBGame game = gameNode.Game;

            AddBagToNode(gameNode, game.GroupItems);
            if (null != game.GroupBags)
            {
                foreach (CCBBag bag in game.GroupBags)
                    AddBagToNode(gameNode, bag);
            }
        }
        private void ResetEntitiesList()
        {
            lbEntities.Items.Clear();
        }
        private void ShowCharacters(CCBGame gameFrom)
        {
            ResetEntitiesList();
            lbEntities.Items.Add(string.Format("{0}:", gameFrom.Name));
            foreach (CCBCharacter character in gameFrom.Characters)
                lbEntities.Items.Add(string.Format("  {0}", character.ToString()));
        }
        private void ShowProperties(CCBCharacter characterFrom)
        {
            ResetEntitiesList();
            foreach (CCBCharacterProperty property in characterFrom.PropertyList)
                lbEntities.Items.Add(property.ToString());
        }
        private void ShowItems(CCBBag bag)
        {
            ResetEntitiesList();
            foreach (CCBBagItem item in bag.Items)
                lbEntities.Items.Add(item.Item);
        }
        #endregion //SelectionViewHelpers

        #region Testers
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            Names.CharacterNameGenerators nameGenerators = new Names.CharacterNameGenerators(); 
            Names.CharacterNames names = nameGenerators.GetWesternFemaleNameGenerator();
            Random rnd = new Random();

            tbLastError.Text = "Word: " + names.GenerateRandomName(rnd);
        }
        #endregion //Testers

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
                        CCBTreeViewCharacter characterNode = currentGameNode.Add(newCharacter);

                        currentGameNode.Game.AddCharacter(newCharacter);
                        characterNode.StartBulkEdit();
                        AddPropertiesToCharacterNode(characterNode);
                        AddBagsToCharacterNode(characterNode);
                        characterNode.EndBulkEdit();
                    }
                    break;
                }
                case EEditMode.em_AddGame:
                {
                    CCBGame newGame = m_games.AddGame(tbItem.Text);
                    CCBTreeViewGame gameNode = new CCBTreeViewGame(newGame);

                    tvGames.Items.Add(gameNode);
                    AddBagToNode(gameNode, newGame.GroupItems);
                    AddOrMoveAdder();
                    break;
                }
                case EEditMode.em_AddProperty:
                {
                    CCBTreeViewCharacter characterNode = FindCharacterFromNode(editMode.Node);
                    CCBTreeViewGame gameNode = FindGameFromNode(editMode.Node);

                    if (null == characterNode)
                    {
                        tbLastError.Text = "Internal error[p]: Cannot find character node.";
                        return;
                    }
                    CCBCharacterProperty newProperty = characterNode.Character.AddProperty(tbItem.Text, tbValue.Text);

                    characterNode.Add(newProperty);
                    if (null != gameNode)
                    {
                        CCBGame game = gameNode.Game;

                        game.AddPropertyToTemplate(newProperty);
                    }
                    break;
                }
                case EEditMode.em_AddBag:
                {
                    CCBTreeViewCharacter characterNode = FindCharacterFromNode(editMode.Node);

                    if (null == characterNode)
                    {
                        tbLastError.Text = "Internal error[b]: Cannot find node.";
                        return;
                    }
                    CCBBag newBag = characterNode.Character.AddBag(tbItem.Text);

                    characterNode.Add(newBag);
                    break;
                }
                case EEditMode.em_AddBagItem:
                {
                    CCBTreeViewBag bagNode = FindBagFromNode(editMode.Node);
                    bool isCountable = true == cbCountable.IsChecked;
                    CCBBagItem newItem;

                    if (null == bagNode)
                    {
                        tbLastError.Text = "Internal error[b]: Cannot find bag node.";
                        return;
                    }
                    if (true == cbCountable.IsChecked)
                    {
                        int count = 1;

                        if (!Int32.TryParse(tbValue.Text, out count))
                            tbLastError.Text = string.Format("Use number. Could not parse [{0}]", tbValue.Text);
                        newItem = bagNode.Bag.AddCountableItem(tbItem.Text, count);
                    }
                    else
                        newItem = bagNode.Bag.AddItem(tbItem.Text);
                    bagNode.Add(newItem);
                    break;
                }
                case EEditMode.em_ModifyCharacter:
                    if (null == editMode.Node)
                    {
                        tbLastError.Text = "Internal error[mc]: No edit node.";
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
                case EEditMode.em_ModifyProperty:
                    if (null == editMode.Node)
                    {
                        tbLastError.Text = "Internal error[mp]: No edit node.";
                        return;
                    }
                    editMode.Node.Header = tbItem.Text;
                    editMode.Node.Property.Name = tbItem.Text;
                    editMode.Node.Property.Value = tbValue.Text;
                    break;
                case EEditMode.em_ModifyBag:
                    if (null == editMode.Node)
                    {
                        tbLastError.Text = "No bag node.";
                        return;
                    }
                    editMode.Node.Header = tbItem.Text;
                    editMode.Node.Bag.Name = tbItem.Text;
                    break;
                case EEditMode.em_ModifyBagItem:
                    if (null == editMode.Node)
                    {
                        tbLastError.Text = "No bag item node.";
                        return;
                    }
                    editMode.Node.Header = tbItem.Text;
                    editMode.Node.BagItem.Item = tbItem.Text;
                    if (editMode.Node.BagItem.IsCountable)
                    {
                        int count;

                        if (Int32.TryParse(tbValue.Text, out count))
                            editMode.Node.BagItem.Count = count;
                        else
                            tbLastError.Text = string.Format("{0} not a number", tbValue.Text);
                    }
                    break;
                default:
                    tbLastError.Text = "Unknown mode.";
                    break;
            }
        }

        private void OpenTemplates()
        {
            CCBTreeViewItem selItem = (CCBTreeViewItem)tvGames.SelectedItem;
            CCBGame gameModel = null;

            if (null != selItem)
            {
                CCBTreeViewGame gameNode = FindGameFromNode(selItem);

                gameModel = gameNode.Game;
            }
            Window templatePickerWnd = new GameTemplatePicker(gameModel, m_onCreateNewGameD, m_onCreateNewTemplateD, m_games.TemplateList);

            templatePickerWnd.Show();
        }

        private void SetDefaultView()
        {
            gbItemView.Header = "Modify";
            btnSave.Content = "Save";
            btnSave.IsEnabled = true;
            tbItem.Text = "";
            tbItem.IsEnabled = true;
            tbValue.Text = "";
            tbValue.IsEnabled = false;
            btnDelete.IsEnabled = false;
            cbCountable.Visibility = System.Windows.Visibility.Hidden;
            btnBagPicker.IsEnabled = false;
            btnTest.Visibility = System.Windows.Visibility.Hidden;
        }
        private EEditMode AddCharacterView()
        {
            SetDefaultView();
            gbItemView.Header = "Add Character";
            btnSave.Content = "Add";
            tbItem.Text = "New Hero";
            ResetEntitiesList();
            return EEditMode.em_AddCharacter;
        }
        private EEditMode AddGameView()
        {
            SetDefaultView();
            gbItemView.Header = "Add Game";
            btnSave.Content = "Add";
            tbItem.Text = "New Game";
            ResetEntitiesList();
            return EEditMode.em_AddGame;
        }
        private EEditMode AddPropertyView()
        {
            SetDefaultView();
            gbItemView.Header = "Add Property";
            btnSave.Content = "Add";
            tbItem.Text = "New Property";
            tbValue.IsEnabled = true;
            ResetEntitiesList();
            return EEditMode.em_AddProperty;
        }
        private EEditMode AddBagView()
        {
            SetDefaultView();
            gbItemView.Header = "Add Bag";
            btnSave.Content = "Add";
            tbItem.Text = "New Bag";
            ResetEntitiesList();
            return EEditMode.em_AddBag;
        }
        private EEditMode AddBagItemView()
        {
            SetDefaultView();
            gbItemView.Header = "Add Bag Item";
            btnSave.Content = "Add";
            tbItem.Text = "New Item";
            ResetEntitiesList();
            cbCountable.Visibility = System.Windows.Visibility.Visible;
            cbCountable.IsEnabled = true;
            cbCountable.IsChecked = false;
            return EEditMode.em_AddBagItem;
        }
        private EEditMode ModifyCharacterView(CCBCharacter character)
        {
            SetDefaultView();
            gbItemView.Header = "Modify Character";
            if (null != character)
                tbItem.Text = character.Name;
            btnDelete.IsEnabled = true;
            btnTemplates.IsEnabled = true;
            ShowProperties(character);
            return EEditMode.em_ModifyCharacter;
        }
        private EEditMode ModifyGameView(CCBGame game)
        {
            SetDefaultView();
            gbItemView.Header = "Modify Game";
            if (null != game)
                tbItem.Text = game.Name;
            btnDelete.IsEnabled = true;
            btnTemplates.IsEnabled = true;
            ShowCharacters(game);
            return EEditMode.em_ModifyGame;
        }
        private EEditMode ModifyPropertyView(CCBCharacterProperty property)
        {
            SetDefaultView();
            gbItemView.Header = "Modify Property";
            tbValue.IsEnabled = true;
            if (null != property)
            {
                tbItem.Text = property.Name;
                tbValue.Text = property.Value;
            }
            btnDelete.IsEnabled = true;
            btnTemplates.IsEnabled = true;
            ResetEntitiesList();
            return EEditMode.em_ModifyProperty;
        }
        private EEditMode ModifyBagView(CCBBag bag)
        {
            SetDefaultView();
            if (null != bag)
                tbItem.Text = bag.Name;
            if (bag.IsLocked)
            {
                gbItemView.Header = "View Bag";
                btnSave.IsEnabled = false;
                tbItem.IsEnabled = false;
            }
            else
            {
                gbItemView.Header = "Modify Bag";
                btnDelete.IsEnabled = true;
            }
            tbValue.IsEnabled = true;
            ShowItems(bag);
            btnBagPicker.IsEnabled = true;
            btnTemplates.IsEnabled = true;
            return EEditMode.em_ModifyBag;
        }
        private EEditMode ModifyBagItemView(CCBBagItem bagItem)
        {
            SetDefaultView();
            gbItemView.Header = "Modify Bag Item";
            if (null != bagItem)
                tbItem.Text = bagItem.Item;
            btnDelete.IsEnabled = true;
            ResetEntitiesList();
            if (bagItem.IsCountable)
            {
                CCBCountedBagItem countedBagItem = (CCBCountedBagItem)bagItem;

                cbCountable.IsChecked = true;
                tbValue.Text = countedBagItem.Count.ToString();
                tbValue.IsEnabled = true;
            }
            else
            {
                cbCountable.IsChecked = false;
            }
            cbCountable.IsEnabled = false;
            cbCountable.Visibility = System.Windows.Visibility.Visible;
            btnBagPicker.IsEnabled = true;
            btnTemplates.IsEnabled = true;
            return EEditMode.em_ModifyBagItem;
        }
        private void OnItemSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            CCBTreeViewItem selItem = GetSelectedNode();

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
                        break;
                    case CCBItemType.itpGame:
                        em.EditMode = ModifyGameView(selItem.Game);
                        break;
                    case CCBItemType.itpBag:
                        em.EditMode = ModifyBagView(selItem.Bag);
                        break;
                    case CCBItemType.itpBagItem:
                        em.EditMode = ModifyBagItemView(selItem.BagItem);
                        break;
                    case CCBItemType.itpGameAdder:
                        em.EditMode = AddGameView();
                        break;
                    case CCBItemType.itpCharacterAdder:
                        em.EditMode = AddCharacterView();
                        break;
                    case CCBItemType.itpProperty:
                        em.EditMode = ModifyPropertyView(selItem.Property);
                        break;
                    case CCBItemType.itpPropertyAdder:
                        em.EditMode = AddPropertyView();
                        break;
                    case CCBItemType.itpBagAdder:
                        em.EditMode = AddBagView();
                        break;
                    case CCBItemType.itpBagItemAdder:
                        em.EditMode = AddBagItemView();
                        break;
                }
                CEditModeProperty.SetEditNode(tbItem, em);
                tbItem.SelectAll();
                //Cannot set focus here, so post event.
                Dispatcher.Invoke(m_onAddingNewEntityModeD);
            }
        }

        private void OnBagPickerClicked(object sender, RoutedEventArgs evt)
        {
            CCBTreeViewGame gameNode = FindGameFromNode(GetSelectedNode());
            CCBTreeViewBag bagNode = FindBagFromNode(GetSelectedNode());

            System.Diagnostics.Debug.Assert(null != bagNode);
            BagItemPicker bagPickerWnd = new BagItemPicker(new BagItemPicker.BagInfo(bagNode.ID, bagNode.Bag, gameNode.Game.GetAllBags(bagNode.Bag)));

            bagPickerWnd.CopyBagItemsCallback = new DOnCopyBagItems(OnCopyBagItems);
            bagPickerWnd.DeleteBagItemsCallback = new DOnDeleteBagItems(OnDeleteBagItems);
            bagPickerWnd.Show();
        }

        private void OnGameTemplatesClicked(object sender, RoutedEventArgs e)
        {
            OpenTemplates();
        }

        private void btnNamePicker_Click(object sender, RoutedEventArgs e)
        {
            NamePicker namePickerWnd = new NamePicker();

            namePickerWnd.CopyNameCallback = new DOnCopyName(OnCopyName);
            namePickerWnd.Show();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            tbLastError.Text = "Export now";
        }
        private void btnTemplates_Click(object sender, RoutedEventArgs e)
        {
            OpenTemplates();
        }
    }
}
