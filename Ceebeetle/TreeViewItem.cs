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
        itpBagAdder,
        itpBagItemAdder,
        itpGame,
        itpCharacter,
        itpBag,
        itpBagItem,
        itpCharacterItems,
        itpProperty
    }

    public class CCBTreeViewItem : TreeViewItem
    {
        private readonly CCBItemType m_itp;
        object m_data;
        private bool m_quickEdit;
        private static uint m_kNodeId = 1;
        private readonly uint m_nodeId;

        public CCBCharacter Character
        {
            get { return (CCBCharacter)m_data; }
        }
        public CCBGame Game
        {
            get { return (CCBGame)m_data; }
        }
        public CCBCharacterProperty Property
        {
            get { return (CCBCharacterProperty)m_data; }
        }
        public CCBBag Bag
        {
            get { return (CCBBag)m_data; }
        }
        public CCBBagItem BagItem
        {
            get { return (CCBBagItem)m_data; }
        }
        public CCBItemType ItemType
        {
            get { return m_itp; }
        }
        protected virtual CCBTreeViewItem Adder
        {
            get { return null; }
        }
        protected bool QuickEdit
        {
            get { return m_quickEdit; }
        }
        public uint ID
        {
            get { return m_nodeId; }
        }

        private CCBTreeViewItem()
            : base()
        {
            m_itp = CCBItemType.itpNone;
            m_nodeId = m_kNodeId++;
        }
        public CCBTreeViewItem(CCBItemType itp)
            : base()
        {
            m_itp = itp;
            m_nodeId = m_kNodeId++;
        }
        public CCBTreeViewItem(CCBItemType itp, string name)
            : base()
        {
            m_itp = itp;
            m_quickEdit = true;
            this.Header = name;
            m_nodeId = m_kNodeId++;
        }
        public CCBTreeViewItem(CCBCharacter character)
            : base()
        {
            m_itp = CCBItemType.itpCharacter;
            m_quickEdit = true;
            this.Header = character.Name;
            this.m_data = character;
            m_nodeId = m_kNodeId++;
        }
        public CCBTreeViewItem(CCBGame game)
            : base()
        {
            m_itp = CCBItemType.itpGame;
            m_quickEdit = true;
            this.Header = game.Name;
            this.m_data = game;
            m_nodeId = m_kNodeId++;
        }
        public CCBTreeViewItem(CCBCharacterProperty property)
            : base()
        {
            m_itp = CCBItemType.itpProperty;
            m_quickEdit = true;
            this.Header = property.Name;
            this.m_data = property;
            m_nodeId = m_kNodeId++;
        }
        public CCBTreeViewItem(CCBBag bag)
        {
            m_itp = CCBItemType.itpBag;
            m_quickEdit = true;
            this.Header = bag.Name;
            this.m_data = bag;
            m_nodeId = m_kNodeId++;
        }
        public CCBTreeViewItem(CCBBagItem item)
        {
            m_itp = CCBItemType.itpBagItem;
            m_quickEdit = true;
            this.Header = item.Item;
            this.m_data = item;
            m_nodeId = m_kNodeId++;
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
        protected virtual void AddOrMoveAdder()
        {
            if (m_quickEdit)
            {
                base.Items.Remove(Adder);
                base.Items.Add(Adder);
            }
        }

        public virtual CCBTreeViewBag Add(CCBBag bag)
        {
            CCBTreeViewBag bagNode = new CCBTreeViewBag(bag);

            base.Items.Add(bagNode);
            return bagNode;
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
    class CCBTreeViewBagAdder : CCBTreeViewItem
    {
        public CCBTreeViewBagAdder()
            : base(CCBItemType.itpBagAdder, "+ add bag")
        {
            this.FontStyle = FontStyles.Italic;
        }
        public CCBTreeViewBagAdder(string header)
            : base(CCBItemType.itpBagAdder, header)
        {
            this.FontStyle = FontStyles.Italic;
        }
    }
    class CCBTreeViewBagItemAdder : CCBTreeViewItem
    {
        public CCBTreeViewBagItemAdder()
            : base(CCBItemType.itpBagItemAdder, "+ add item")
        {
            this.FontStyle = FontStyles.Italic;
        }
        public CCBTreeViewBagItemAdder(string header)
            : base(CCBItemType.itpBagItemAdder, header)
        {
            this.FontStyle = FontStyles.Italic;
        }
    }

    class CCBTreeViewGame : CCBTreeViewItem
    {
        private CCBTreeViewCharacterAdder m_characterAdder;

        protected override CCBTreeViewItem Adder
        {
            get { return m_characterAdder; }
        }

        public CCBTreeViewGame(string name)
            : base(CCBItemType.itpGame)
        {
            m_characterAdder = new CCBTreeViewCharacterAdder();
            base.Items.Add(m_characterAdder);
        }
        public CCBTreeViewGame(CCBGame game)
            : base(game)
        {
            m_characterAdder = new CCBTreeViewCharacterAdder();
            base.Items.Add(m_characterAdder);
        }
        public CCBTreeViewCharacter Add(CCBCharacter character)
        {
            CCBTreeViewCharacter newNode = new CCBTreeViewCharacter(character);

            base.Items.Add(newNode);
            AddOrMoveAdder();
            return newNode;
        }
    }

    public class CCBTreeViewBag : CCBTreeViewItem
    {
        private CCBTreeViewBagItemAdder m_itemAdder;

        protected override CCBTreeViewItem Adder
        {
            get { return m_itemAdder; }
        }

        public CCBTreeViewBag(CCBBag bag)
            : base(bag)
        {
            m_itemAdder = new CCBTreeViewBagItemAdder();
            base.Items.Add(m_itemAdder);
        }

        public CCBTreeViewItem Add(CCBBagItem item)
        {
            CCBTreeViewItem newNode = new CCBTreeViewItem(item);

            base.Items.Add(newNode);
            AddOrMoveAdder();
            return newNode;
        }
        public bool Remove(string itemToFind)
        {
            if (null != itemToFind) foreach (CCBTreeViewItem itemNode in base.Items)
            {
                CCBBagItem itemToCompare = itemNode.BagItem;

                if (itemToCompare.Equals(itemToFind))
                {
                    base.Items.Remove(itemNode);
                    return true;
                }
            }
            return false;
        }
    }

    class CCBTreeViewCharacter : CCBTreeViewItem
    {
        private CCBTreeViewPropertyAdder m_propertyAdder;
        private CCBTreeViewBagAdder m_bagAdder;

        protected override CCBTreeViewItem Adder
        {
            get
            {
                return m_propertyAdder;
            }
        }
        public CCBTreeViewCharacter(string name)
            : base(CCBItemType.itpCharacter)
        {
            m_propertyAdder = new CCBTreeViewPropertyAdder();
            m_bagAdder = new CCBTreeViewBagAdder();
            base.Items.Add(m_propertyAdder);
        }
        public CCBTreeViewCharacter(CCBCharacter character)
            : base(character)
        {
            m_propertyAdder = new CCBTreeViewPropertyAdder();
            m_bagAdder = new CCBTreeViewBagAdder();
            base.Items.Add(m_propertyAdder);
        }

        protected override void AddOrMoveAdder()
        {
            if (QuickEdit)
            {
                base.Items.Remove(m_propertyAdder);
                base.Items.Remove(m_bagAdder);
                base.Items.Add(m_propertyAdder);
                base.Items.Add(m_bagAdder);
            }
        }
        public CCBTreeViewItem Add(CCBCharacterProperty property)
        {
            CCBTreeViewItem newNode = new CCBTreeViewItem(property);

            base.Items.Add(newNode);
            AddOrMoveAdder();
            return newNode;
        }
        public override CCBTreeViewBag Add(CCBBag bag)
        {
            CCBTreeViewBag newNode = new CCBTreeViewBag(bag);

            base.Items.Add(newNode);
            AddOrMoveAdder();
            return newNode;
        }
    }
}
