using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Ceebeetle
{
    public enum CPType //CharacterPropertyType
    {
        cpt_None = 0,
        cpt_Normal,
        cpt_Numeric
    }

    [DataContract(Name="PropertyTemplate")]
    [KnownType(typeof(CCBCharacterProperty))]
    public class CCBCharacterPropertyTemplate : IEquatable<CCBCharacterPropertyTemplate>, IComparable<CCBCharacterPropertyTemplate>
    {
        [DataMember(Name="Type")] private CPType m_cpt;
        [DataMember(Name="Name")] private string m_name;

        public CPType Type
        {
            get { return m_cpt; }
            set { m_cpt = value; }
        }
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        //Overrides for equality and comparisons
        #region Comparisons
        public static bool operator ==(CCBCharacterPropertyTemplate lhs, CCBCharacterPropertyTemplate rhs)
        {
            if (ReferenceEquals(lhs, null) && ReferenceEquals(rhs, null)) return true;
            if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return false;
            return 0 == string.Compare(lhs.m_name, rhs.m_name);
        }
        public static bool operator !=(CCBCharacterPropertyTemplate lhs, CCBCharacterPropertyTemplate rhs)
        {
            if (ReferenceEquals(lhs, null) && ReferenceEquals(rhs, null)) return false;
            if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return true;
            return 0 == string.Compare(lhs.m_name, rhs.m_name);
        }
        public override bool Equals(object obj)
        {
            if (obj is CCBCharacterPropertyTemplate)
            {
                CCBCharacterPropertyTemplate propt = (CCBCharacterPropertyTemplate)obj;

                if (null != propt)
                    return m_name.Equals(propt.m_name);
            }
            return m_name.Equals(obj);
        }
        public override int GetHashCode()
        {
            return m_name.GetHashCode();
        }
        //IEquatable
        public bool Equals(CCBCharacterPropertyTemplate rhs)
        {
            return m_name.Equals(rhs.m_name);
        }
        //IComparable
        public int CompareTo(CCBCharacterPropertyTemplate rhs)
        {
            return m_name.CompareTo(rhs.m_name);
        }
        #endregion

        public CCBCharacterPropertyTemplate()
        {
            m_cpt = CPType.cpt_Normal;
            m_name = "Unknown Property";
        }
        public CCBCharacterPropertyTemplate(CPType type)
        {
            m_cpt = type;
            m_name = "Unknown Property";
        }
        public CCBCharacterPropertyTemplate(string name, CPType type)
        {
            m_cpt = type;
            m_name = name;
        }
        public CCBCharacterPropertyTemplate(string name)
        {
            m_cpt = CPType.cpt_Normal;
            m_name = name;
        }
        public CCBCharacterPropertyTemplate(CCBCharacterPropertyTemplate template)
        {
            m_cpt = template.m_cpt;
            m_name = template.m_name;
        }
    }

    [DataContract(Name = "CharacterProperty")]
    public class CCBCharacterProperty : CCBCharacterPropertyTemplate
    {
        [DataMember(Name="Value")]
        private object m_value;

        public string Value
        {
            get { return m_value.ToString(); }
            set { m_value = value; }
        }
        public int IntValue
        {
            get
            {
                if (CPType.cpt_Numeric == Type)
                    return (int)m_value;
                return -4973;
            }
        }

        public CCBCharacterProperty() : base()
        {
            m_value = "";
        }
        public CCBCharacterProperty(string name, string value) : base(name)
        {
            m_value = value;
        }
        public CCBCharacterProperty(string name, int value) : base(name, CPType.cpt_Numeric)
        {
            m_value = (object) value;
        }
        public CCBCharacterProperty(CCBCharacterPropertyTemplate template) : base(template)
        {
            m_value = "";
        }

        public override string ToString()
        {
 	         return string.Format("{0}:{1}", Name, m_value);
        }
    }

    #region PredicateHelper
    //Helper class for Predicates
    public class ComparePropertyToName
    {
        private readonly string m_name;
        public ComparePropertyToName(string name)
        {
            m_name = name;
        }
        private ComparePropertyToName() {}
        public Predicate<CCBCharacterPropertyTemplate> GetPredicate
        {
            get { return IsThisProperty; }
        }
        private bool IsThisProperty(CCBCharacterPropertyTemplate property)
        {
            return m_name == property.Name;
        }
    }
    #endregion

    [CollectionDataContract(Name = "PropertyTemplateList")]
    public class CharacterPropertyTemplateList : List<CCBCharacterPropertyTemplate>
    {
        public CharacterPropertyTemplateList()
            : base()
        {
        }

        public int AddFrom(CharacterPropertyList propertyList)
        {
            int cBefore = this.Count;

            foreach (CCBCharacterProperty property in propertyList)
            {
                if (!Contains(property.Name))
                    this.Add(new CCBCharacterPropertyTemplate(property));
            }
            System.Diagnostics.Debug.Assert(this.Count >= cBefore);
            return this.Count - cBefore;
        }
        public bool AddNew(CCBCharacterPropertyTemplate property)
        {
            if (!Contains(property.Name))
            {
                Add(property);
                return true;
            }
            return false;
        }
        public bool Remove(string propertyName)
        {
            CCBCharacterPropertyTemplate property = Find(propertyName);

            if (null != property)
            {
                base.Remove(property);
                return true;
            }
            return false;
        }

        public CCBCharacterPropertyTemplate Find(string name)
        {
            ComparePropertyToName comparer = new ComparePropertyToName(name);
            return base.Find(comparer.GetPredicate);
        }
        public bool Contains(string name)
        {
            return null != Find(name);
        }
    }
    [CollectionDataContract(Name = "CharacterPropertyList")]
    public class CharacterPropertyList : List<CCBCharacterProperty>
    {
        public CharacterPropertyList()
            : base()
        {
        }

        public CCBCharacterProperty Find(string name)
        {
            ComparePropertyToName comparer = new ComparePropertyToName(name);
            return base.Find(comparer.GetPredicate);
        }
        public bool Contains(string name)
        {
            return (null != Find(name));
        }

        public void AddTemplateProperties(CharacterPropertyTemplateList templateList)
        {
            foreach (CCBCharacterPropertyTemplate templateProperty in templateList)
            {
                Add(new CCBCharacterProperty(templateProperty));
            }
        }

        public int GetLongestNameLen(int minlen = 0)
        {
            int len = minlen;

            foreach (CCBCharacterProperty prop in this)
            {
                if (len < prop.Name.Length)
                    len = prop.Name.Length;
            }
            return len;
        }
    }
}
