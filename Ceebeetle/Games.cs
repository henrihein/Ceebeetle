using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Threading;
using System.IO;

namespace Ceebeetle
{
    [DataContract(Name="GameTemplate")]
    public class CCBGameTemplate
    {
        [DataMember(Name="TemplateName")]
        private string m_name;
        [DataMember(Name="TemplateProperties")]
        private CharacterPropertyTemplateList m_propertyList;
        [DataMember(Name="TemplateBags")]
        private CCBBags m_bags;

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
        public CharacterPropertyTemplateList PropertyTemplateList
        {
            get { return m_propertyList; }
        }
        public CCBBags Bags
        {
            get { return m_bags; }
        }

        public CCBGameTemplate()
        {
            m_name = "";
            m_propertyList = new CharacterPropertyTemplateList();
            m_bags = new CCBBags();
        }
        public CCBGameTemplate(string name)
        {
            m_name = name;
            m_propertyList = new CharacterPropertyTemplateList();
            m_bags = new CCBBags();
        }
        public CCBGameTemplate(string name, CCBGame gameFrom)
        {
            m_name = name;
            m_propertyList = new CharacterPropertyTemplateList();
            m_bags = new CCBBags();
            Rebase(gameFrom);
        }
        public CCBGameTemplate(CCBGame gameFrom)
        {
            m_name = gameFrom.Name + " Template";
            m_propertyList = new CharacterPropertyTemplateList();
            m_bags = new CCBBags();
            Rebase(gameFrom);
        }

        public override string ToString()
        {
            return m_name;
        }

        public void Rebase(CCBGame gameFrom)
        {
            m_propertyList.Clear();
            foreach (CCBCharacter character in gameFrom.Characters)
                m_propertyList.AddFrom(character.PropertyList);
            m_bags.Clear();
            if (null != gameFrom.GroupBags)
                foreach (CCBBag bag in gameFrom.GroupBags)
                    m_bags.Add(bag);
        }

        public CCBBag AddBag(string name)
        {
            CCBBag newBag = new CCBBag(name);

            if (null == m_bags)
                m_bags = new CCBBags();
            m_bags.Add(newBag);
            return newBag;
        }
        public CCBBag AddBag(CCBBag bag)
        {
            if (null == m_bags)
                m_bags = new CCBBags();
            m_bags.Add(bag);
            return bag;
        }
    }

    [DataContract(Name = "Game")]
    public class CCBGame
    {
        static readonly string m_kGroupItemLabel = "Group Items";

        [DataMember(Name = "Name")]
        private string m_name;
        [DataMember(Name = "Characters")]
        private CCBCharacterList m_characters;
        [DataMember(Name = "GroupItems")]
        private CCBBag m_groupItems;
        [DataMember(Name = "GroupBags")]
        private CCBBags m_groupBags;
        [DataMember(Name="GameTemplateProperties")]
        private CharacterPropertyTemplateList m_propertyTemplateList;

        public string Name
        {
            get { return m_name; }
            set
            {
                CCBDirty.kDirty = true;
                m_name = value;
            }
        }
        public CCBCharacterList Characters
        {
            get { return m_characters; }
        }
        public CCBBag GroupItems
        {
            get { return m_groupItems; }
        }
        public CCBBags GroupBags
        {
            get { return m_groupBags; }
        }

        public CCBGame()
        {
            m_name = "My RPG";
            m_characters = new CCBCharacterList();
            m_groupItems = new CCBBag(m_kGroupItemLabel);
            m_groupBags = new CCBBags();
            m_propertyTemplateList = new CharacterPropertyTemplateList();
        }
        public CCBGame(string name)
        {
            m_name = name;
            m_characters = new CCBCharacterList();
            m_groupItems = new CCBBag(m_kGroupItemLabel);
            m_groupBags = new CCBBags();
            m_propertyTemplateList = new CharacterPropertyTemplateList();
        }
        public CCBGame(string name, CCBGameTemplate templateFrom)
        {
            m_name = name;
            m_characters = new CCBCharacterList();
            m_groupItems = new CCBBag(m_kGroupItemLabel);
            m_groupBags = new CCBBags();
            m_propertyTemplateList = new CharacterPropertyTemplateList();
            foreach (CCBCharacterPropertyTemplate templateProperty in templateFrom.PropertyTemplateList)
                m_propertyTemplateList.Add(new CCBCharacterPropertyTemplate(templateProperty));
            foreach (CCBBag bag in templateFrom.Bags)
                m_groupBags.Add(new CCBBag(bag));
        }

        public override string ToString()
        {
            return m_name;
        }

        public void AddCharacter(CCBCharacter newCharacter)
        {
            CCBDirty.kDirty = true;
            if (null != m_propertyTemplateList)
                newCharacter.AddPropertiesFromTemplate(m_propertyTemplateList);
            m_characters.Add(newCharacter);
        }
        public void DeleteCharacter(CCBCharacter delCharacter)
        {
            CCBDirty.kDirty = true;
            m_characters.DeleteSafe(delCharacter);
        }
        public void AddGroupItem(string item)
        {
            CCBDirty.kDirty = true;
            m_groupItems.AddItem(item);
        }
        public bool RemoveGroupItem(string item)
        {
            CCBDirty.kDirty = true;
            return m_groupItems.RemoveItem(item);
        }

        //Template guff
        public void CheckPropertyForDeletion(string propertyName)
        {
            foreach (CCBCharacter character in m_characters)
            {
                if (character.PropertyList.Contains(propertyName))
                    return;
            }
            m_propertyTemplateList.Remove(propertyName);
        }
        public void AddPropertyToTemplate(CCBCharacterProperty property)
        {
            if (!m_propertyTemplateList.Contains(property.Name))
                m_propertyTemplateList.AddNew(new CCBCharacterPropertyTemplate(property.Name));
        }

        public CCBBag[] GetAllBags(CCBBag bagExcept)
        {
            List<CCBBag> allBags = new List<CCBBag>();

            allBags.Add(m_groupItems);
            if (null != m_groupBags)
                foreach (CCBBag groupBag in m_groupBags)
                    if (!ReferenceEquals(groupBag, bagExcept)) 
                        allBags.Add(groupBag);
            foreach (CCBCharacter character in m_characters)
            {
                if (!ReferenceEquals(character.Items, bagExcept))
                    allBags.Add(new CCBOwnedBag(character, character.Items));
                foreach (CCBBag characterBag in character.BagList)
                    if (!ReferenceEquals(characterBag, bagExcept)) 
                        allBags.Add(new CCBOwnedBag(character, characterBag));
            }
            return allBags.ToArray();
        }
    }

    [CollectionDataContract(Name = "GameTemplateList")]
    public class CCBGameTemplateList : List<CCBGameTemplate>
    {
    }

    [CollectionDataContract(Name = "Games")]
    public class CCBGames : List<CCBGame>
    {
    }

    [DataContract(Name="GameData")]
    public class CCBGameData : IEnumerable
    {
        [DataMember(Name="GameTemplates")]
        CCBGameTemplateList m_templates;
        [DataMember(Name="Games")]
        CCBGames m_games;

        public CCBGameData()
        {
            CCBDirty.kDirty = false;
            m_templates = new CCBGameTemplateList();
            m_games = new CCBGames();
        }

        public bool IsDirty
        {
            get { return CCBDirty.kDirty; }
        }
        public IEnumerator GetEnumerator()
        {
            return m_games.GetEnumerator();
        }
        public CCBGameTemplateList TemplateList
        {
            get { return m_templates; }
        }

        public CCBGame AddGame(string name)
        {
            return AddGame(name, null);
        }
        public CCBGame AddGame(string name, CCBGameTemplate template)
        {
            CCBGame newGame = (null == template) ? new CCBGame(name) : new CCBGame(name, template);

            CCBDirty.kDirty = true;
            AddSafe(newGame);
            return newGame;
        }
        public CCBGameTemplate AddTemplate(string name, CCBGame gameFrom)
        {
            CCBGameTemplate newTemplate = (null == gameFrom) ? new CCBGameTemplate(name) : new CCBGameTemplate(name, gameFrom);

            CCBDirty.kDirty = true;
            AddSafe(newTemplate);
            return newTemplate;
        }
        public void DeleteGame(CCBGame game)
        {
            CCBDirty.kDirty = true;
            m_games.Remove(game);
        }
        public void DeleteGameSafe(CCBGame game)
        {
            CCBDirty.kDirty = true;
            lock (this)
            {
                m_games.Remove(game);
            }
        }
        public void AddSafe(CCBGame game)
        {
            CCBDirty.kDirty = true;
            lock (this)
            {
                m_games.Add(game);
            }
        }
        public void AddSafe(CCBGameTemplate template)
        {
            lock (this)
            {
                if (null == m_templates)
                    m_templates = new CCBGameTemplateList();
                m_templates.Add(template);
            }
        }
        public string AsXML()
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(CCBGames));
            StringWriter sww = new StringWriter();

            lock (this)
            {
                xsSubmit.Serialize(sww, this);
            }
            return sww.ToString();
        }
        public CCBGame[] GetGames()
        {
            return m_games.ToArray();
        }
        public CCBGameTemplate[] GetGameTemplates()
        {
            return m_templates.ToArray();
        }

        public void LoadGames(object sender, DoWorkEventArgs evtArgs)
        {
            BackgroundWorker wSender = (BackgroundWorker)sender;
            CCBConfig config = (CCBConfig)evtArgs.Argument;
            string docPath;

            if (null != wSender)
            {
                try
                {
                    wSender.ReportProgress(1);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write("ReportProgress: " + ex.ToString());
                }
            }
            if (null == config)
            {
                System.Diagnostics.Debug.Write("No config found. Not loading games.");
                evtArgs.Result = TStatusUpdate.tsuFileNothingLoaded;
                return;
            }
            else
            {
                docPath = config.DocPath;
                if (!System.IO.File.Exists(docPath))
                {
                    docPath = config.GetLoadFile();
                    if (!System.IO.File.Exists(docPath))
                    {
                        System.Diagnostics.Debug.Write("Load file does not exist. Not loading games.");
                        evtArgs.Result = TStatusUpdate.tsuFileNothingLoaded;
                        return;
                    }
                }
            }
            System.Diagnostics.Debug.Write("Loading:" + docPath);
            lock (this)
            {
                XmlReader xsReader = null;
                try
                {
                    xsReader = XmlReader.Create(docPath);
                    DataContractSerializer dsReader = new DataContractSerializer(typeof(CCBGameData));
                    CCBGameData loadedGames = (CCBGameData)dsReader.ReadObject(xsReader);

                    xsReader.Close();
                    foreach (CCBGame game in loadedGames.m_games)
                    {
                        AddSafe(game);
                    }
                    foreach (CCBGameTemplate template in loadedGames.m_templates)
                        AddSafe(template);
                    evtArgs.Result = TStatusUpdate.tsuFileLoaded;
                }
                catch (System.IO.FileNotFoundException nothere)
                {
                    System.Diagnostics.Debug.Write(String.Format("No data file, not loading games [{0}]", nothere.FileName));
                    evtArgs.Result = TStatusUpdate.tsuFileNothingLoaded;
                    if (null != xsReader)
                        xsReader.Close();
                }
                catch (System.Runtime.Serialization.SerializationException serex)
                {
                    System.Diagnostics.Debug.Write(String.Format("XML parsing error, not loading games [{0}]", serex.ToString()));
                    evtArgs.Result = TStatusUpdate.tsuParseError;
                    if (null != xsReader)
                        xsReader.Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write("Exception reading: " + ex.ToString());
                    evtArgs.Result = TStatusUpdate.tsuError;
                    if (null != xsReader)
                        xsReader.Close();
                }
            }
        }
        public void SaveGames(object sender, DoWorkEventArgs evtArgs)
        {
            try
            {
                if (SaveGames(evtArgs.Argument))
                    evtArgs.Result = TStatusUpdate.tsuFileSaved;
                else
                    evtArgs.Result = TStatusUpdate.tsuError;
            }
            catch (IOException iox)
            {
                System.Diagnostics.Debug.Write(iox.ToString());
                evtArgs.Cancel = true;
            }
        }
        public bool SaveGames(object oConfig)
        {
            CCBConfig conf = (CCBConfig)oConfig;

            if (null != conf)
            {
                if (SaveGames(conf.TmpPath))
                {
                    try
                    {
                        System.IO.File.Copy(conf.TmpPath, conf.DocPath, true);
                        return true;
                    }
                    catch (System.IO.IOException ioex)
                    {
                        System.Diagnostics.Debug.Write("Error copying file: " + ioex.ToString());
                    }
                }
            }
            return false;
        }
        public bool SaveGames(string path)
        {
            XmlWriter xmlWriter = null;

            lock (this)
            {
                try
                {
                    DataContractSerializer dsWriter = new DataContractSerializer(typeof(CCBGameData));
                    
                    xmlWriter = XmlWriter.Create(path);
                    dsWriter.WriteObject(xmlWriter, this);
                    xmlWriter.Flush();
                    xmlWriter.Close();
                    return true;
                }
                catch (IOException ioex)
                {
                    System.Diagnostics.Debug.Write("IO Exception saving: " + ioex.ToString());
                }
                catch (XmlException xmlex)
                {
                    System.Diagnostics.Debug.Write("XML Exception saving: " + xmlex.ToString());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write("Exception saving: " + ex.ToString());
                }
            }
            if (null != xmlWriter)
                xmlWriter.Close();
            return false;
        }
    }
}
