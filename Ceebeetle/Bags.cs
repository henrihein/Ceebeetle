﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Ceebeetle
{
    [DataContract(Name="BagItem", Namespace=@"http://www.w3.org/2001/XMLSchema")]
    [KnownType(typeof(CCBCountedBagItem))]
    public class CCBBagItem : IEquatable<CCBBagItem>, IComparable<CCBBagItem>
    {
        [DataMember(Name="Item")]
        private string m_item;

        public string Item
        {
            get { return m_item; }
            set { m_item = value; }
        }
        public virtual bool IsCountable
        {
            get { return false; }
        }
        public virtual int Count
        {
            get { return 0; }
            set { }
        }

        public CCBBagItem()
        {
            m_item = "item";
        }
        public CCBBagItem(string item)
        {
            m_item = item;
        }

        #region Comparisons
        public static bool operator ==(CCBBagItem lhs, CCBBagItem rhs)
        {
            if (ReferenceEquals(lhs, null) && ReferenceEquals(rhs, null)) return true;
            if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return false;
            return 0 == string.Compare(lhs.m_item, rhs.m_item);
        }
        public static bool operator !=(CCBBagItem lhs, CCBBagItem rhs)
        {
            if (ReferenceEquals(lhs, null) && ReferenceEquals(rhs, null)) return false;
            if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return true;
            return 0 == string.Compare(lhs.m_item, rhs.m_item);
        }
        public override bool Equals(object obj)
        {
            if (obj is CCBBagItem)
            {
                CCBBagItem item = (CCBBagItem)obj;

                if (null != item)
                    return m_item.Equals(item.m_item);
            }
            return m_item.Equals(obj);
        }
        public override int GetHashCode()
        {
            return m_item.GetHashCode();
        }
        //IEquatable
        public bool Equals(CCBBagItem rhs)
        {
            return m_item.Equals(rhs.m_item);
        }
        //IComparable
        public int CompareTo(CCBBagItem rhs)
        {
            return m_item.CompareTo(rhs.m_item);
        }
        #endregion

        public override string ToString()
        {
            return m_item.ToString();
        }
    }

    [DataContract(Name = "CountedBagItem", Namespace = @"http://www.w3.org/2001/XMLSchema")]
    public class CCBCountedBagItem : CCBBagItem
    {
        [DataMember]
        private int m_count;

        public override int Count
        {
            get { return m_count; }
            set { m_count = value; }
        }
        public override bool IsCountable
        {
            get { return true; }
        }

        public CCBCountedBagItem() : base()
        {
            m_count = 1;
        }
        public CCBCountedBagItem(int count) : base()
        {
            m_count = count;
        }
        public CCBCountedBagItem(string item, int count) : base(item)
        {
            m_count = count;
        }
    }

    #region PredicateHelper
    //Helper classes for Predicates
    public class CompareBagItemToName
    {
        private readonly string m_name;
        public CompareBagItemToName(string name)
        {
            m_name = name;
        }
        private CompareBagItemToName() { }
        public Predicate<CCBBagItem> GetPredicate
        {
            get { return IsThisItem; }
        }
        private bool IsThisItem(CCBBagItem item)
        {
            if (null == item)
                return false;
            return 0 == string.Compare(m_name, item.Item);
        }
    }
    public class CompareBagToName
    {
        private readonly string m_name;
        public CompareBagToName(string name)
        {
            m_name = name;
        }
        private CompareBagToName() { }
        public Predicate<CCBBag> GetPredicate
        {
            get { return IsThisBag; }
        }
        private bool IsThisBag(CCBBag bag)
        {
            if (null == bag)
                return false;
            return 0 == string.Compare(m_name, bag.Name);
        }
    }
    #endregion

    [DataContract(Name = "Bag")]
    [KnownType(typeof(CCBLockedBag))]
    public class CCBBag
    {
        [DataMember(Name = "BagName")]
        private string m_name;
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
        [DataMember(Name = "BagItems")]
        private List<CCBBagItem> m_items;
        public List<CCBBagItem> Items
        {
            get
            {
                if (null == m_items) m_items = new List<CCBBagItem>();
                return m_items;
            }
        }
        public virtual bool IsLocked
        {
            get { return false; }
        }

        public CCBBag() : base()
        {
            m_name = "Items";
            m_items = new List<CCBBagItem>();
        }
        public CCBBag(string name) : base()
        {
            m_name = name;
            m_items = new List<CCBBagItem>();
        }

        public CCBBagItem AddItem(string item)
        {
            CCBBagItem bagItem = new CCBBagItem(item);

            if (null == m_items)
                m_items = new List<CCBBagItem>();
            m_items.Add(bagItem);
            return bagItem;
        }
        public CCBBagItem AddCountableItem(string item, int value)
        {
            CCBCountedBagItem bagItem = new CCBCountedBagItem(item, value);

            if (null == m_items)
                m_items = new List<CCBBagItem>();
            m_items.Add(bagItem);
            return bagItem;
        }
        public CCBBagItem Find(string name)
        {
            if (null != m_items)
            {
                CompareBagItemToName comparer = new CompareBagItemToName(name);
                return m_items.Find(comparer.GetPredicate);
            }
            return null;
        }
        public bool RemoveItem(string item)
        {
            return RemoveItem(Find(item));
        }
        public bool RemoveItem(CCBBagItem item)
        {
            if (null == m_items)
                return false;
            if (!ReferenceEquals(null, item))
                return m_items.Remove(item);
            return false;
        }
    }
    //Bag with fixed name
    [DataContract(Name = "FixedBag")]
    class CCBLockedBag : CCBBag
    {
        public CCBLockedBag() : base()
        {
        }
        public CCBLockedBag(string name) : base(name)
        {
        }

        public override bool IsLocked
        {
            get { return true; }
        }
    }

    [CollectionDataContract(Name = "Bags", Namespace = @"http://www.w3.org/2001/XMLSchema")]
    public class CCBBags : List<CCBBag>
    {
        public bool Remove(string name)
        {
            CompareBagToName comparer = new CompareBagToName(name);
            CCBBag bagToRemove = base.Find(comparer.GetPredicate);

            if (null != bagToRemove)
                return base.Remove(bagToRemove);
            return false;
        }
    }
}
