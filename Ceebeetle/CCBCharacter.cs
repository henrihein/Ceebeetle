using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Threading;

namespace Ceebeetle
{
    public class CCBDirty
    {
        public static bool kDirty;
    }

    [DataContract(Name = "Character", Namespace = @"http://www.w3.org/2001/XMLSchema")]
    public class CCBCharacter
    {
        static uint m_nextId = 1;

        [DataMember(Name="Name")]
        private string m_name;
        public string Name
        {
            get { return m_name; }
            set {
                CCBDirty.kDirty = true;
                m_name = value;
            }
        }
        public readonly uint     m_id;

        [DataMember(Name="PropertyList")]
        private CharacterPropertyList m_propertyList;
        public CharacterPropertyList PropertyList
        {
            get { return m_propertyList; }
        }

        [DataMember(Name = "Items")]
        private CCBBag m_items;
        public CCBBag Items
        {
            get { return m_items; }
        }

        [DataMember(Name = "CharacterBags")]
        private CCBBags m_bags;
        public CCBBags BagList
        {
            get { return m_bags; }
        }

        public CCBCharacter()
        {
            m_id = m_nextId++;
            m_name = System.String.Format("NewCharacter{0}", m_id);
            m_propertyList = new CharacterPropertyList();
            m_items = new CCBLockedBag("Items");
            m_bags = new CCBBags();
        }
        public CCBCharacter(string name)
        {
            m_id = m_nextId++;
            m_name = name;
            m_propertyList = new CharacterPropertyList();
            m_items = new CCBLockedBag("Items");
            m_bags = new CCBBags();
        }
        public override string ToString()
        {
            return m_name;
        }
        public override int GetHashCode()
        {
            return (int)m_id;
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public bool Equals(CCBCharacter rhs)
        {
            return rhs.m_id == m_id;
        }
        public static bool operator==(CCBCharacter lhs, CCBCharacter rhs)
        {
            if (ReferenceEquals(lhs, null) && ReferenceEquals(rhs, null)) return true;
            if (ReferenceEquals(lhs, null)) return false;
            if (ReferenceEquals(rhs, null)) return false;
            return lhs.m_id == rhs.m_id;
        }
        public static bool operator !=(CCBCharacter lhs, CCBCharacter rhs)
        {
            if (ReferenceEquals(lhs, null) && ReferenceEquals(rhs, null)) return false;
            if (ReferenceEquals(lhs, null)) return true;
            if (ReferenceEquals(rhs, null)) return true;
            return lhs.m_id != rhs.m_id;
        }

        //Properties
        public CCBCharacterProperty AddProperty(string name, string value)
        {
            CCBCharacterProperty newProperty = new CCBCharacterProperty(name, value);

            CCBDirty.kDirty = true;
            m_propertyList.Add(newProperty);
            return newProperty;
        }
        public void RemovePropertySafe(CCBCharacterProperty property)
        {
            if (null != property) lock (this)
            {
                CCBDirty.kDirty = true;
                m_propertyList.Remove(property);
            }
        }
        public void RemoveProperty(string name)
        {
            CCBCharacterProperty property = m_propertyList.Find(name);

            RemovePropertySafe(property);
        }

        public CCBBag AddBag(string name)
        {
            CCBBag newBag = new CCBBag(name);

            CCBDirty.kDirty = true;
            if (null == m_bags)
                m_bags = new CCBBags();
            m_bags.Add(newBag);
            return newBag;
        }
        public void RemoveBag(string name)
        {
            if (null != m_bags)
                m_bags.Remove(name);
        }
    }

    [CollectionDataContract(Name = "Characters", Namespace = @"http://www.w3.org/2001/XMLSchema")]
    public class CCBCharacterList : List<CCBCharacter>
    {
        public CCBCharacterList() : base()
        {
        }

        public void AddSafe(CCBCharacter character)
        {
            CCBDirty.kDirty = true;
            lock (this)
            {
                base.Add(character);
            }
        }
        public void DeleteSafe(CCBCharacter character)
        {
            CCBDirty.kDirty = true;
            lock (this)
            {
                base.Remove(character);
            }
        }
        public void DeleteSafe(List<Object> list)
        {
            lock (this)
            {
                foreach (object obj in list)
                {
                    CCBCharacter chararacter = (CCBCharacter)obj;

                    if (chararacter == null)
                        throw new Exception("Internal error: non-character in character list.");
                    if (base.Contains(chararacter))
                    {
                        base.Remove(chararacter);
                    }
                }
            }
            CCBDirty.kDirty = true;
        }
    }

    [DataContract(Name = "Game", Namespace = @"http://www.w3.org/2001/XMLSchema")]
    public class CCBGame
    {
        [DataMember(Name="Name")]
        private string m_name;
        [DataMember(Name="Characters")]
        private CCBCharacterList m_characters;
        public string Name
        {
            get { return m_name; }
            set {
                CCBDirty.kDirty = true;
                m_name = value;
            }
        }
        public CCBCharacterList Characters
        {
            get { return m_characters; }
        }
        public CCBGame()
        {
            m_name = "My RPG";
            m_characters = new CCBCharacterList();
        }
        public CCBGame(string name)
        {
            m_name = name;
            m_characters = new CCBCharacterList();
        }
        public void AddCharacter(CCBCharacter newCharacter)
        {
            CCBDirty.kDirty = true;
            m_characters.Add(newCharacter);
        }
        public void DeleteCharacter(CCBCharacter delCharacter)
        {
            m_characters.DeleteSafe(delCharacter);
        }
    }
    [CollectionDataContract(Name = "Games", Namespace = @"http://www.w3.org/2001/XMLSchema")]
    public class CCBGames : List<CCBGame>
    {
        public CCBGames() : base()
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
            try
            {
                XmlReader xsReader = XmlReader.Create(docPath);
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write("Exception reading: " + ex.ToString());
                evtArgs.Result = TStatusUpdate.tsuError;
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
