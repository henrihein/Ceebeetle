using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Runtime.Serialization;

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
            if (0 == m_propertyList.Count)
                return m_name;
            return string.Format("{0} [{1}]", m_name, m_propertyList[0].ToString());
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
        public void RemoveBag(CCBBag bag)
        {
            if (null != m_bags)
                m_bags.Remove(bag);
        }

        public void AddPropertiesFromTemplate(CharacterPropertyTemplateList templateList)
        {
            m_propertyList.AddTemplateProperties(templateList);
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

}
