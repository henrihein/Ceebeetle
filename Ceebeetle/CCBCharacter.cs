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

        public CCBCharacter()
        {
            m_id = m_nextId++;
            m_name = System.String.Format("NewCharacter{0}", m_id);
            m_propertyList = new CharacterPropertyList();
        }
        public CCBCharacter(string name)
        {
            m_id = m_nextId++;
            m_name = name;
            m_propertyList = new CharacterPropertyList();
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
            m_propertyList.AddSafe(newProperty);
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
            CCBCharacterProperty property = m_propertyList.FindSafe(name);

            RemovePropertySafe(property);
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
            //XmlSerializer xsReader = new XmlSerializer(typeof(CCBGames));
            BackgroundWorker wSender = (BackgroundWorker)sender;

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
            System.Diagnostics.Debug.Write("Loading:" + evtArgs.Argument.ToString());
            try
            {
                string xmlDocPath = evtArgs.Argument.ToString();
                XmlReader xsReader = XmlReader.Create(xmlDocPath);
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
                //string xmlData = AsXML();
                XmlWriter xmlWriter = XmlWriter.Create(path);

                dsWriter.WriteObject(xmlWriter, this);
                xmlWriter.Flush();
                xmlWriter.Close();
            }
        }
    }
}
