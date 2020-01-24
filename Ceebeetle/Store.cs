using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Ceebeetle
{
    [DataContract(Name = "StoreItem")]
    public class CCBStoreItem : CCBCountedBagItem
    {
        private int m_cost;
        private int m_limit;
        public int Cost
        {
            get { return m_cost; }
            set { m_cost = value; }
        }
        public int Limit
        {
            get { return m_limit; }
            set { m_limit = value; }
        }

        private CCBStoreItem()
        {
        }
        public CCBStoreItem(string name) : base(name, 0)
        {
            m_limit = -1;
        }
    }

    [DataContract(Name = "PotentialStoreItem")]
    public class CCBPotentialStoreItem : CCBStoreItem
    {
        private int[] m_costRange;
        private int m_chance;
        private bool m_randomizeLimit;
        public int MinCost
        {
            get { return m_costRange[0]; }
            set { m_costRange[0] = value; }
        }
        public int MaxCost
        {
            get { return m_costRange[1]; }
            set { m_costRange[1] = value; }
        }
        public int Chance
        {
            get { return m_chance; }
            set { m_chance = value; }
        }
        public bool RandomizeLimit
        {
            get { return m_randomizeLimit; }
            set { m_randomizeLimit = value; }
        }

        private CCBPotentialStoreItem() : base("n/a")
        {
            m_costRange = new int[2] { 0, 1 };
            m_chance = 100;
            m_randomizeLimit = false;
        }
        public CCBPotentialStoreItem(string name) : base(name)
        {
            m_costRange = new int[2] { 0, 1 };
            m_chance = 100;
            m_randomizeLimit = false;
        }

        public bool CompareStats(CCBPotentialStoreItem rhs)
        {
            if (m_chance != rhs.m_chance) return false;
            if (m_costRange[0] != rhs.m_costRange[0]) return false;
            if (m_costRange[1] != rhs.m_costRange[1]) return false;
            if (m_randomizeLimit != rhs.m_randomizeLimit) return false;
            if (Limit != rhs.Limit) return false;
            return true;
        }
    }

    [DataContract(Name = "StoreItemPlaceType")]
    public class CCBStoreItemPlaceType
    {
        private string m_placeName;
        private CCBBag m_items;

        public string Name
        {
            get { return m_placeName; }
            set { m_placeName = value; }
        }

        private CCBStoreItemPlaceType()
        {
        }
        public CCBStoreItemPlaceType(string name)
        {
            m_placeName = name;
            m_items = new CCBBag();
        }
        public override string ToString()
        {
            return m_placeName;
        }

        public void AddPotentialStoreItem(CCBPotentialStoreItem item)
        {
            m_items.Add(item);
        }
        public bool RemovePotentialStoreItem(CCBPotentialStoreItem item)
        {
            CCBBagItem foundItem = m_items.Find(item.Item);

            if (null == foundItem)
                return false;
            m_items.RemoveItem(foundItem);
            return true;
        }
        public CCBPotentialStoreItem FindItem(string name)
        {
            CCBBagItem item = m_items.Find(name);

            if (null != item)
            {
                CCBPotentialStoreItem storeItem = (CCBPotentialStoreItem)item;

                System.Diagnostics.Debug.Assert(null != storeItem);
                return storeItem;
            }
            return null;
        }
    }

    [CollectionDataContract(Name = "StoreItemPlaceTypes")]
    public class CCBStoreItemPlaceTypeList : List<CCBStoreItemPlaceType>
    {
    }

    [DataContract(Name = "Store")]
    public class CCBStore : CCBBag
    {
        public CCBStore() : base()
        {
        }
    }
}
