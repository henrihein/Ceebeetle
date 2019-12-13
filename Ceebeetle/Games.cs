using System;
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
        public CCBGameTemplate()
        {
        }
        public CCBGameTemplate(string name)
        {
            m_name = name;
            m_propertyList = new CharacterPropertyTemplateList();
            m_bags = new CCBBags();
        }
        public CCBGameTemplate(CCBGame gameFrom)
        {
            m_name = gameFrom.Name + " Template";
            m_propertyList = new CharacterPropertyTemplateList();
            m_bags = new CCBBags();
            Rebase(gameFrom);
        }

        public void Rebase(CCBGame gameFrom)
        {
            m_propertyList.Clear();
            foreach (CCBCharacter character in gameFrom.Characters)
                m_propertyList.AddFrom(character.PropertyList);
            m_bags.Clear();
            foreach (CCBBag bag in gameFrom.GroupBags)
                m_bags.Add(bag);
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
            m_propertyTemplateList = new CharacterPropertyTemplateList();
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
    }
    [CollectionDataContract(Name = "Games")]
    public class CCBGames : List<CCBGame>
    {
        public CCBGames()
            : base()
        {
            CCBDirty.kDirty = false;
        }

        public bool IsDirty
        {
            get { return CCBDirty.kDirty; }
        }

        public CCBGame AddGame(string name)
        {
            CCBGame newGame = new CCBGame(name);

            CCBDirty.kDirty = true;
            base.Add(newGame);
            return newGame;
        }
        public void DeleteGame(CCBGame game)
        {
            CCBDirty.kDirty = true;
            base.Remove(game);
        }
        public void DeleteGameSafe(CCBGame game)
        {
            CCBDirty.kDirty = true;
            lock (this)
            {
                base.Remove(game);
            }
        }
        public void AddSafe(CCBGame game)
        {
            CCBDirty.kDirty = true;
            lock (this)
            {
                base.Add(game);
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
            XmlReader xsReader = null;
            try
            {
                xsReader = XmlReader.Create(docPath);
                DataContractSerializer dsReader = new DataContractSerializer(typeof(CCBGames));
                CCBGames loadedGames = (CCBGames)dsReader.ReadObject(xsReader);

                xsReader.Close();
                foreach (CCBGame game in loadedGames)
                {
                    AddSafe(game);
                }
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
        public void SaveGames(object sender, DoWorkEventArgs evtArgs)
        {
            try
            {
                SaveGames(evtArgs.Argument.ToString());
                evtArgs.Result = TStatusUpdate.tsuFileSaved;
            }
            catch (IOException iox)
            {
                System.Diagnostics.Debug.Write(iox.ToString());
                evtArgs.Cancel = true;
            }
        }
        public void SaveGames(string path)
        {
            lock (this)
            {
                DataContractSerializer dsWriter = new DataContractSerializer(typeof(CCBGames));
                XmlWriter xmlWriter = XmlWriter.Create(path);

                dsWriter.WriteObject(xmlWriter, this);
                xmlWriter.Flush();
                xmlWriter.Close();
            }
        }
    }
}
