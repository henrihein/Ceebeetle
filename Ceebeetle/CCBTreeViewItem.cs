using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Ceebeetle
{
    public enum CCBItemType
    {
        itpNone = 0,
        itpGameAdder,
        itpCharacterAdder,
        itpPropertyAdder,
        itpGame,
        itpCharacter,
        itpBag,
        itpProperty
    }

    public class CCBTreeViewItem : TreeViewItem
    {
        private readonly CCBItemType m_itp;
        object m_data;

        static CCBTreeViewItem()
        {
        }
        public CCBCharacter Character
        {
            get { return (CCBCharacter)m_data; }
        }
        public CCBGame Game
        {
            get { return (CCBGame)m_data; }
        }
        public CCBItemType ItemType
        {
            get { return m_itp; }
        }
        private CCBTreeViewItem()
            : base()
        {
            m_itp = CCBItemType.itpNone;
        }
        public CCBTreeViewItem(CCBItemType itp)
            : base()
        {
            m_itp = itp;
        }
        public CCBTreeViewItem(CCBItemType itp, string name)
            : base()
        {
            m_itp = itp;
            this.Header = name;
        }
        public CCBTreeViewItem(CCBCharacter character)
            : base()
        {
            m_itp = CCBItemType.itpCharacter;
            this.Header = character.Name;
            this.m_data = character;
        }
        public CCBTreeViewItem(CCBGame game)
            : base()
        {
            m_itp = CCBItemType.itpGame;
            this.Header = game.Name;
            this.m_data = game;
        }
        public CCBTreeViewItem(string name, CCBCharacterProperty property)
            : base()
        {
            m_itp = CCBItemType.itpProperty;
            this.Header = name;
            this.m_data = property;
        }
    }
    class CCBTreeViewGameAdder : CCBTreeViewItem
    {
        public CCBTreeViewGameAdder()
            : base(CCBItemType.itpGameAdder, "+ add game")
        {
            this.FontStyle = FontStyles.Italic;
        }
        public CCBTreeViewGameAdder(string header)
            : base(CCBItemType.itpGameAdder, header)
        {
            this.FontStyle = FontStyles.Italic;
        }
    }
    class CCBTreeViewCharacterAdder : CCBTreeViewItem
    {
        public CCBTreeViewCharacterAdder()
            : base(CCBItemType.itpCharacterAdder, "+ add character")
        {
            this.FontStyle = FontStyles.Italic;
        }
        public CCBTreeViewCharacterAdder(string header)
            : base(CCBItemType.itpCharacterAdder, header)
        {
            this.FontStyle = FontStyles.Italic;
        }
    }
    class CCBTreeViewPropertyAdder : CCBTreeViewItem
    {
        public CCBTreeViewPropertyAdder()
            : base(CCBItemType.itpPropertyAdder, "+ add property")
        {
            this.FontStyle = FontStyles.Italic;
        }
        public CCBTreeViewPropertyAdder(string header)
            : base(CCBItemType.itpPropertyAdder, header)
        {
            this.FontStyle = FontStyles.Italic;
        }
    }

    class CCBTreeViewGame : CCBTreeViewItem
    {
        private CCBTreeViewCharacterAdder m_characterAdder;
        private bool m_quickEdit;

        public CCBTreeViewGame(string name)
            : base(CCBItemType.itpGame)
        {
            m_characterAdder = new CCBTreeViewCharacterAdder();
            m_quickEdit = true;
            base.Items.Add(m_characterAdder);
        }
        public CCBTreeViewGame(CCBGame game)
            : base(game)
        {
            m_characterAdder = new CCBTreeViewCharacterAdder();
            m_quickEdit = true;
            base.Items.Add(m_characterAdder);
        }

        public void StartBulkEdit()
        {
            m_quickEdit = false;
        }
        public void EndBulkEdit()
        {
            m_quickEdit = true;
            AddOrMoveAdder();
        }
        protected void AddOrMoveAdder()
        {
            if (m_quickEdit)
            {
                base.Items.Remove(m_characterAdder);
                base.Items.Add(m_characterAdder);
            }
        }
        public CCBTreeViewItem Add(CCBCharacter character)
        {
            CCBTreeViewItem newNode = new CCBTreeViewItem(character);

            base.Items.Add(newNode);
            AddOrMoveAdder();
            return newNode;
        }
    }
    class CCBTreeViewCharacter : CCBTreeViewItem
    {
        private CCBTreeViewPropertyAdder m_propertyAdder;
        private bool m_quickEdit;

        public CCBTreeViewCharacter(string name)
            : base(CCBItemType.itpCharacter)
        {
            m_propertyAdder = new CCBTreeViewPropertyAdder();
            m_quickEdit = false;
            base.Items.Add(m_propertyAdder);
        }
        public CCBTreeViewCharacter(CCBCharacter character)
            : base(character)
        {
            m_propertyAdder = new CCBTreeViewPropertyAdder();
            m_quickEdit = false;
            base.Items.Add(m_propertyAdder);
        }

        public void StartBulkEdit()
        {
            m_quickEdit = false;
        }
        public void EndBulkEdit()
        {
            m_quickEdit = true;
            AddOrMoveAdder();
        }
        protected void AddOrMoveAdder()
        {
            if (m_quickEdit)
            {
                base.Items.Remove(m_propertyAdder);
                base.Items.Add(m_propertyAdder);
            }
        }
        public CCBTreeViewItem Add(string propertyName, CCBCharacterProperty property)
        {
            CCBTreeViewItem newNode = new CCBTreeViewItem(propertyName, property);

            base.Items.Add(newNode);
            AddOrMoveAdder();
            return newNode;
        }

    }
}
