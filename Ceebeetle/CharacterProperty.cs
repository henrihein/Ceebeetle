using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace Ceebeetle
{
    public enum CPType //CharacterPropertyType
    {
        cpt_None = 0,
        cpt_Normal,
        cpt_Numeric
    }

    public class CCBCharacterPropertyTemplate
    {
        private CPType m_cpt;
        private string m_name;
        static int m_propId = 1;

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

        public CCBCharacterPropertyTemplate()
        {
            m_cpt = CPType.cpt_Normal;
            m_name = string.Format("Property {0}", m_propId++);
        }
        public CCBCharacterPropertyTemplate(CPType type)
        {
            m_cpt = type;
            m_name = string.Format("Property {0}", m_propId++);
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

    public class CCBCharacterProperty : CCBCharacterPropertyTemplate
    {
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
        public CCBCharacterProperty(string value) : base()
        {
            m_value = value;
        }
        public CCBCharacterProperty(int value) : base(CPType.cpt_Numeric)
        {
            m_value = (object) value;
        }
        public CCBCharacterProperty(CCBCharacterPropertyTemplate template) : base(template)
        {
            m_value = "";
        }
    }

    public class CharacterPropertyTemplateList : List<CCBCharacterPropertyTemplate>
    {
        public CharacterPropertyTemplateList()
            : base()
        {
        }
    }
    public class CharacterPropertyList : List<CCBCharacterProperty>
    {
        public CharacterPropertyList()
            : base()
        {
        }
    }
}
