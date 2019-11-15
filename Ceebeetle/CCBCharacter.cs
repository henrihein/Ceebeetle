using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Xml.Serialization;
using System.Threading;

namespace Ceebeetle
{
    public class CCBCharacter
    {
        static uint m_nextId = 1;

        public string m_name { get; set; }
        public readonly uint     m_id;

        public CCBCharacter()
        {
            m_id = m_nextId++;
            m_name = System.String.Format("NewCharacter{0}", m_id);
        }
        public CCBCharacter(string name)
        {
            m_id = m_nextId++;
            m_name = name;
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
    }

    public class CCBCharacterList : List<CCBCharacter>
    {
        private bool m_dirty;

        public CCBCharacterList() : base()
        {
        }

        public bool IsDirty()
        {
            return m_dirty;
        }
        public void AddSafe(CCBCharacter character)
        {
            m_dirty = true;
            lock(this)
            {
                base.Add(character);
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
                        m_dirty = true;
                        base.Remove(chararacter);
                    }
                }
            }
        }
        public string AsXML()
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(CCBCharacterList));
            StringWriter sww = new StringWriter();

            lock (this)
            {
                xsSubmit.Serialize(sww, this);
            }
            return sww.ToString();
        }
        public void SaveCharacters(object sender, DoWorkEventArgs evtArgs)
        {
            string xmlData = AsXML();

            try
            {
                System.IO.StreamWriter writer = new System.IO.StreamWriter(evtArgs.Argument.ToString());

                writer.Write(xmlData);
                writer.Flush();
                writer.Close();
            }
            catch (IOException iox)
            {
                System.Diagnostics.Debug.Write(iox.ToString());
                evtArgs.Cancel = true;
            }
        }
        public void LoadCharacters(object sender, DoWorkEventArgs evtArgs)
        {
            XmlSerializer xsReader = new XmlSerializer(typeof(CCBCharacterList));

            System.Diagnostics.Debug.Write("Loading:" + evtArgs.Argument.ToString());
            try
            {
                System.IO.StreamReader reader = new System.IO.StreamReader(evtArgs.Argument.ToString());
                CCBCharacterList loadedCharacters = (CCBCharacterList)xsReader.Deserialize(reader);

                reader.Close();
                foreach (CCBCharacter character in loadedCharacters)
                {
                    AddSafe(character);
                }
                evtArgs.Result = TStatusUpdate.tsuFileLoaded;
            }
            catch (System.IO.FileNotFoundException nothere)
            {
                System.Diagnostics.Debug.Write(String.Format("No data file, not loading characters [{0}]", nothere.FileName));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write("Exception reading: " + ex.ToString());
                evtArgs.Result = TStatusUpdate.tsuError;
            }
        }
    }
}
