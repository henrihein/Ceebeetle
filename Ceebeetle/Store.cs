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
using System.Windows;
using System.Windows.Media;
using System.Globalization;

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

        protected CCBStoreItem()
        {
        }
        public CCBStoreItem(string name)
            : base(name, 0)
        {
        }
    }

    //Base on StoreItem rather than BagItem, for type safety in the UI code.
    public class CCBStoreItemOmitted : CCBStoreItem
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
        [DataMember(Name = "Available")]
        private bool m_available;
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
        public bool Available
        {
            get { return m_available; }
            set { m_available = value; }
        }

        private CCBPotentialStoreItem()
            : base("n/a")
        {
            m_costRange = new int[2] { 0, 1 };
            m_chance = 100;
            m_randomizeLimit = false;
        }
        public CCBPotentialStoreItem(string name)
            : base(name)
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
        public void MergeItems(CCBStorePlaceType placeFrom)
        {
            foreach (CCBPotentialStoreItem item in placeFrom.m_items.Items)
                AddPotentialStoreItem(item);
        }
    }

    [CollectionDataContract(Name = "StoreItemPlaceTypes")]
    public class CCBStorePlaceTypeList : List<CCBStorePlaceType>
    {
        private static string m_allPlaceName = "All";
        private CCBStorePlaceType m_allPlaces;
        public CCBStorePlaceType AllPlaces
        {
            get { return m_allPlaces; }
        }

        public CCBStorePlaceTypeList() : base()
        {
            m_allPlaces = new CCBStorePlaceType(m_allPlaceName);
            Add(m_allPlaces);
        }
        public void MergePlaces(CCBStorePlaceTypeList places)
        {
            foreach (CCBStorePlaceType place in places)
            {
                if (places.IsAllPlaceType(place))
                    m_allPlaces.MergeItems(places.AllPlaces);
                else
                    MergePlace(place);
            }
        }
        private bool IsAllPlaceType(CCBStorePlaceType place)
        {
            return 0 == string.Compare(place.Name, m_allPlaceName);
        }
        private void MergePlace(CCBStorePlaceType newPlace)
        {
            foreach (CCBStorePlaceType oldPlace in this)
            {
                if (0 == string.Compare(newPlace.Name, oldPlace.Name))
                {
                    oldPlace.MergeItems(newPlace);
                    return;
                }
            }
            Add(newPlace);
        }
    }

    [DataContract(Name = "StoreManager")]
    public class CCBStoreManager
    {
        [DataMember(Name = "PlaceList")]
        private CCBStorePlaceTypeList m_places;
        [DataMember(Name = "Stores")]
        private List<CCBStore> m_stores;
        static private bool m_dirty = false;

        public CCBStorePlaceTypeList Places
        {
            get 
            { 
                m_dirty = true;  
                return m_places; 
            }
        }
        public List<CCBStore> Stores
        {
            get { return m_stores; }
        }
        public bool Dirty
        {
            get { return m_dirty; }
        }

        public CCBStoreManager()
        {
            m_places = new CCBStorePlaceTypeList();
            m_stores = new List<CCBStore>();
        }

        public CCBStorePlaceType AddPlaceType(string placeTypeName)
        {
            CCBStorePlaceType newPlaceType = new CCBStorePlaceType(placeTypeName);

            m_places.Add(newPlaceType);
            return newPlaceType;
        }
        private void MergeStores(List<CCBStore> stores)
        {
            foreach (CCBStore store in stores)
            {
                if (!m_stores.Contains(store))
                    m_stores.Add(store);
            }
        }
        public bool SaveStores(CCBConfig conf)
        {
            XmlWriter xmlWriter = null;

            lock (this)
            {
                try
                {
                    DataContractSerializer dsWriter = new DataContractSerializer(typeof(CCBStoreManager));

                    conf.MaybeBackup(conf.GetStoreTmpFilePath());
                    xmlWriter = XmlWriter.Create(conf.GetStoreTmpFilePath());
                    dsWriter.WriteObject(xmlWriter, this);
                    xmlWriter.Flush();
                    xmlWriter.Close();
                    m_dirty = false;
                    try
                    {
                        System.IO.File.Copy(conf.GetStoreTmpFilePath(), conf.GetStoreFilePath(), true);
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

                    m_places.MergePlaces(stores.m_places);
                    MergeStores(stores.m_stores);
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

        private void AddItemsToStore(CCBStore store, CCBStorePlaceType place, Random rnd)
        {
            foreach (CCBPotentialStoreItem maybeItem in place.StoreItems.Items)
            {
                if (maybeItem.IncludeInStore(rnd))
                {
                    CCBStoreItem newItem = new CCBStoreItem(maybeItem.Item);

                    newItem.Cost = maybeItem.GetCost(rnd);
                    newItem.Count = maybeItem.GetLimit(rnd);
                    if (0 == newItem.Count)
                        store.Add(new CCBStoreItemOmitted(maybeItem.Item, "Limited"));
                    else
                        store.Add(newItem);
                }
                else
                    store.Add(new CCBStoreItemOmitted(maybeItem.Item, "Chance"));
            }
        }
        public CCBStore AddStore(CCBStorePlaceType place)
        {
            if (null != place)
            {
                CCBStore newStore = new CCBStore("New Store", place.Name);
                System.Random rnd = new Random();

                lock (this)
                {
                    //New items replace previous items, and we want the specific place to override 
                    //the generic All place. So add items from All first.
                    AddItemsToStore(newStore, m_places.AllPlaces, rnd);
                    AddItemsToStore(newStore, place, rnd);
                    m_stores.Add(newStore);
                }
                return newStore;
            }
            return null;
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
    [KnownType(typeof(CCBP2PStore))]
    public class CCBStore : CCBBag
    {
        [DataMember(Name = "StoreType")]
        private string m_storeType;
        public string StoreType
        {
            get { return m_storeType; }
        }

        public CCBStore() : base("Unnamed store")
        {
            m_storeType = "Unknown";
        }
        public CCBStore(string name) : base(name)
        {
            m_storeType = "Unknown";
        }
        public CCBStore(string name, string storeType)
            : base(name)
        {
            m_storeType = storeType;
        }

        public override string  ToString()
        {
            if (null == m_storeType)
                return base.ToString();
            return string.Format("{0} - store in {1}", Name, m_storeType);
        }

        public override CCBBagItem Add(CCBBagItem item)
        {
            //For a store, we don't want duplicate items
            CCBBagItem exists = Find(item.Item);

            if (null != exists)
                RemoveItem(exists);
            return base.Add(item);
        }

        private string BuildItemsString()
        {
            StringBuilder sb = new StringBuilder();
            string lnFormat = "{0,-54}\t{1,24}\t{2, 24}\r\n";
            sb.Append(string.Format(lnFormat, "Item", "Cost", "Item count"));
            foreach (CCBStoreItem item in Items)
            {
                string itemCount;

                if (-1 == item.Count)
                    itemCount = "-";
                else
                    itemCount = string.Format("{0}", item.Count);
                sb.Append(string.Format(lnFormat, item.Item, item.Cost, itemCount));
            }
            return sb.ToString();
        }
        public Visual Print()
        {
            int xPageWidth = 816; //8.5 * 96
            int xLeft = 12, xRight = xPageWidth - xLeft;
            int yTop = 12;
            DrawingVisual printed = new DrawingVisual();
            DrawingContext dc = printed.RenderOpen();
            Brush fgBrush = new SolidColorBrush(Colors.Black);
            string strLangSetting = CultureInfo.CurrentCulture.ToString();
            FormattedText headerText = new FormattedText(Name, CultureInfo.GetCultureInfo(strLangSetting),
                                                            FlowDirection.LeftToRight, new Typeface("Cambria"),
                                                            32, Brushes.Black);
            string strItems = BuildItemsString();
            FormattedText bodyText = new FormattedText(strItems, CultureInfo.GetCultureInfo(strLangSetting),
                                                            FlowDirection.LeftToRight, new Typeface("Cambria"),
                                                            11, Brushes.Black);
            headerText.MaxTextWidth = 300;
            headerText.MaxTextHeight = 240;
            headerText.SetFontSize(36 * (96.0 / 72.0));
            headerText.SetFontWeight(FontWeights.Bold);
            headerText.SetForegroundBrush(fgBrush);
            dc.DrawText(headerText, new Point(xLeft, yTop));
            dc.DrawLine(new Pen(fgBrush, 8), new Point(xLeft, 72), new Point(xRight, 72));
            //bodyText.SetFontSize(11 * (96.0 / 72.0));
            bodyText.SetFontWeight(FontWeights.Normal);
            bodyText.SetTextDecorations(new TextDecorationCollection());
            //Format table header, find the end first
            int ixHeaderEnd = strItems.IndexOf("\r\n");
            if (-1 == ixHeaderEnd)
                ixHeaderEnd = 0;
            else
            {
                bodyText.SetFontWeight(FontWeights.Bold, 0, ixHeaderEnd);
                bodyText.SetTextDecorations(TextDecorations.Underline, 0, ixHeaderEnd);
            }
            dc.DrawText(bodyText, new Point(xLeft, yTop + 80));
            dc.Close();
            return printed;
        }
    }
}
