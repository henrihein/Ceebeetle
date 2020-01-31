using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Threading;
using System.IO;

namespace Ceebeetle
{
    [DataContract(Name = "StoreItem")]
    [KnownType(typeof(CCBPotentialStoreItem))]
    public class CCBStoreItem : CCBCountedBagItem
    {
        [DataMember(Name = "Cost")]
        private int m_cost;
        public int Cost
        {
            get { return m_cost; }
            set { m_cost = value; }
        }

        private CCBStoreItem()
        {
        }
        public CCBStoreItem(string name) : base(name, 0)
        {
        }
    }

    public class CCBStoreItemOmitted : CCBBagItem
    {
        private string m_reason;
        public string Reason
        {
            get { return m_reason; }
        }
        private CCBStoreItemOmitted()
        {
        }
        public CCBStoreItemOmitted(string item, string reason)
            : base(item)
        {
            m_reason = reason;
        }
        public override string ToString()
        {
            return string.Format("{0} ({1})", Item, m_reason);
        }

    }

    [DataContract(Name = "PotentialStoreItem")]
    public class CCBPotentialStoreItem : CCBStoreItem
    {
        [DataMember(Name = "Costs")]
        private int[] m_costRange;
        [DataMember(Name = "Chance")]
        private int m_chance;
        [DataMember(Name = "RandomizeLimit")]
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
            if (Count != rhs.Count) return false;
            return true;
        }
        public bool IncludeInStore(Random rnd)
        {
            if (100 <= m_chance) return true;
            int r = rnd.Next(0, 100);
            return (r < m_chance);
        }
        public int GetCost(Random rnd)
        {
            if ((m_costRange[0] == m_costRange[1]) || (0 == m_costRange[1]))
                return m_costRange[0];
            return rnd.Next(m_costRange[0], m_costRange[1] + 1);
        }
        public int GetLimit(Random rnd)
        {
            if (0 == Count) return 0;
            if (m_randomizeLimit)
                return rnd.Next(0, Count + 1);
            return Count;
        }
    }

    [DataContract(Name = "StoreItemPlaceType")]
    public class CCBStorePlaceType
    {
        [DataMember(Name = "PlaceTypeName")]
        private string m_placeName;
        [DataMember(Name = "Items")]
        private CCBBag m_items;

        public string Name
        {
            get { return m_placeName; }
            set { m_placeName = value; }
        }
        public CCBBag StoreItems
        {
            get { return m_items; }
        }

        private CCBStorePlaceType()
        {
        }
        public CCBStorePlaceType(string name)
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
            //Avoid duplicate items by removing it first if there.
            CCBPotentialStoreItem oldItem = FindItem(item.Item);

            if (null != oldItem)
                m_items.RemoveItem(oldItem);
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
    public class CCBStoreItemPlaceTypeList : List<CCBStorePlaceType>
    {
    }

    [DataContract(Name = "StoreManager")]
    public class CCBStoreManager
    {
        [DataMember(Name = "PlaceList")]
        private CCBStoreItemPlaceTypeList m_places;
        private List<CCBStore> m_stores;
        static private bool m_dirty = false;

        public CCBStoreItemPlaceTypeList Places
        {
            get 
            { 
                m_dirty = true;  
                return m_places; 
            }
        }
        public bool Dirty
        {
            get { return m_dirty; }
        }

        public CCBStoreManager()
        {
            m_places = new CCBStoreItemPlaceTypeList();
            m_stores = new List<CCBStore>();
        }

        public CCBStorePlaceType AddPlaceType(string placeTypeName)
        {
            CCBStorePlaceType newPlaceType = new CCBStorePlaceType(placeTypeName);

            m_places.Add(newPlaceType);
            return newPlaceType;
        }
        public bool SaveStores(string savePath, string tmpPath)
        {
            XmlWriter xmlWriter = null;

            lock (this)
            {
                try
                {
                    DataContractSerializer dsWriter = new DataContractSerializer(typeof(CCBStoreManager));
                    
                    xmlWriter = XmlWriter.Create(tmpPath);
                    dsWriter.WriteObject(xmlWriter, this);
                    xmlWriter.Flush();
                    xmlWriter.Close();
                    m_dirty = false;
                    try
                    {
                        System.IO.File.Copy(tmpPath, savePath, true);
                    }
                    catch (System.IO.IOException ioex)
                    {
                        System.Diagnostics.Debug.Write("Error copying file: " + ioex.ToString());
                    }
                    return true;
                }
                catch (IOException ioex)
                {
                    System.Diagnostics.Debug.Write("IO Exception saving store definitions: " + ioex.ToString());
                }
                catch (XmlException xmlex)
                {
                    System.Diagnostics.Debug.Write("XML Exception saving store definitions: " + xmlex.ToString());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write("Exception saving store definitions: " + ex.ToString());
                }
            }
            if (null != xmlWriter)
                xmlWriter.Close();
            return false;
        }
        public bool LoadStores(string docPath)
        {
            lock (this)
            {
                XmlReader xsReader = null;
                try
                {
                    xsReader = XmlReader.Create(docPath);
                    DataContractSerializer dsReader = new DataContractSerializer(typeof(CCBStoreManager));
                    CCBStoreManager stores = (CCBStoreManager)dsReader.ReadObject(xsReader);

                    m_places = stores.m_places;
                    m_dirty = false;
                    xsReader.Close();
                    return true;
                }
                catch (System.IO.FileNotFoundException nothere)
                {
                    System.Diagnostics.Debug.Write(String.Format("No data file, not loading stores [{0}]", nothere.FileName));
                    if (null != xsReader)
                        xsReader.Close();
                }
                catch (System.Runtime.Serialization.SerializationException serex)
                {
                    System.Diagnostics.Debug.Write(String.Format("XML parsing error, not loading stores [{0}]", serex.ToString()));
                    if (null != xsReader)
                        xsReader.Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write("Exception reading store document: " + ex.ToString());
                    if (null != xsReader)
                        xsReader.Close();
                }
            }
            return false;
        }

        public CCBStore AddStore(CCBStorePlaceType place)
        {
            CCBStore newStore = new CCBStore();
            System.Random rnd = new Random();

            foreach (CCBPotentialStoreItem maybeItem in place.StoreItems.Items)
            {
                if (maybeItem.IncludeInStore(rnd))
                {
                    CCBStoreItem newItem = new CCBStoreItem(maybeItem.Item);

                    newItem.Cost = maybeItem.GetCost(rnd);
                    newItem.Count = maybeItem.GetLimit(rnd);
                    if (0 == newItem.Count)
                        newStore.Add(new CCBStoreItemOmitted(maybeItem.Item, "Limited"));
                    else
                        newStore.Add(newItem);
                }
                else
                    newStore.Add(new CCBStoreItemOmitted(maybeItem.Item, "Chance"));
            }
            lock (this)
            {
                m_stores.Add(newStore);
            }
            return newStore;
        }
        public bool DeleteStore(CCBStore store)
        {
            lock (this)
            {
                if (m_stores.Contains(store))
                {
                    m_stores.Remove(store);
                    return true;
                }
            }
            return false;
        }
    }

    [DataContract(Name = "Store")]
    public class CCBStore : CCBBag
    {
        public CCBStore() : base("Unnamed store")
        {
        }
        public CCBStore(string name) : base(name)
        {
        }
    }
}
