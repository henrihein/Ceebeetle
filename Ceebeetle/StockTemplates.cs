using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ceebeetle
{
    public enum TemplateTypeID
    {
        tti_None = 0,
        tti_User,
        tti_PlayerSelector,
        tti_SwordAndSorcery
    }

    //Helper to add templates as entries to UI lists
    class GameTemplateEntry
    {
        string m_name;
        CCBGameTemplate m_gameTemplate;

        public CCBGameTemplate Template
        {
            get { return m_gameTemplate; }
        }

        private GameTemplateEntry()
        {
        }
        public GameTemplateEntry(string name, CCBGameTemplate template)
        {
            m_name = name;
            m_gameTemplate = template;
        }

        public override string ToString()
        {
            return m_name.ToString();
        }
    }

    public struct CCBStockTemplate
    {
        string m_name;
        TemplateTypeID m_id;
        CCBGameTemplate m_template;
        public string Name
        {
            get { return m_name; }
        }
        public TemplateTypeID ID
        {
            get { return m_id; }
        }
        private void InitializeTemplate()
        {
            switch (m_id)
            {
                case TemplateTypeID.tti_PlayerSelector:
                    m_template = CCBStockTemplates.GetPlayerSelector();
                    break;
                case TemplateTypeID.tti_SwordAndSorcery:
                    m_template = CCBStockTemplates.GetSwordAndSorcery();
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
        }
        public CCBGameTemplate Template
        {
            get { 
                if (null == m_template)
                    InitializeTemplate();
                return m_template;
            }
        }

        public CCBStockTemplate(string name, TemplateTypeID id)
        {
            m_id = id;
            m_name = name;
            m_template = null;
        }
    }
    //Populates stock, or 'known' game templates. Could move this to an XML file when the format has solidified.
    class CCBStockTemplates
    {
        private static List<CCBStockTemplate> m_templateList = new List<CCBStockTemplate>();
        static CCBStockTemplates()
        {
            m_templateList.Add(new CCBStockTemplate("Player Selector", TemplateTypeID.tti_PlayerSelector));
            m_templateList.Add(new CCBStockTemplate("Sword & Sorcery", TemplateTypeID.tti_SwordAndSorcery));
        }

        public static List<CCBStockTemplate> StockTemplateList
        {
            get { return m_templateList; }
        }

        private static void AddPlayersToBag(CCBBag bag, int cPlayers)
        {
            for (int ix = 0; ix < cPlayers; ix++)
                bag.AddItem(string.Format("Player {0}", ix + 1));
        }
        private static CCBBag AddPlayerBag(string bagName, int cPlayers)
        {
            CCBBag playerBag = new CCBBag(bagName);

            AddPlayersToBag(playerBag, cPlayers);
            return playerBag;
        }
        public static CCBGameTemplate GetPlayerSelector()
        {
            CCBGameTemplate playerSelector = new CCBGameTemplate("Player Selector");

            playerSelector.AddBag(AddPlayerBag("Two Players", 2));
            playerSelector.AddBag(AddPlayerBag("Three Players", 3));
            playerSelector.AddBag(AddPlayerBag("Four Players", 4));
            playerSelector.AddBag(AddPlayerBag("Five Players", 5));
            playerSelector.AddBag(AddPlayerBag("Six Players", 6));
            playerSelector.AddBag(AddPlayerBag("Seven Players", 7));
            playerSelector.AddBag(AddPlayerBag("Eight Players", 8));
            playerSelector.AddBag(AddPlayerBag("Nine Players", 9));
            playerSelector.AddBag(AddPlayerBag("Ten Players", 10));
            playerSelector.AddBag(AddPlayerBag("Eleven Players", 11));
            playerSelector.AddBag(AddPlayerBag("Twelve Players", 12));
            return playerSelector;
        }
        public static CCBGameTemplate GetSwordAndSorcery()
        {
            CCBGameTemplate ssTemplate = new CCBGameTemplate("Sword & Sorcery");

            ssTemplate.PropertyTemplateList.Add(new CCBCharacterPropertyTemplate("Soulrank", CPType.cpt_Numeric));
            return ssTemplate;
        }
    }
}
