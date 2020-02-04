﻿using System;
using System.Windows;
using System.Collections.Generic;

namespace Ceebeetle
{
    public class StoreItemViewer
    {
        private CCBStoreItem m_item;
        public CCBStoreItem Item
        {
            get { return m_item; }
        }

        private StoreItemViewer()
        {
        }
        public StoreItemViewer(CCBStoreItem storeItem)
        {
            m_item = storeItem;
        }
        public StoreItemViewer(CCBBagItem bagItem)
        {
            m_item = (CCBStoreItem)bagItem;
        }

        public override string ToString()
        {
            if (null == m_item)
                return "Unknown";
            if (-1 == m_item.Count)
                return string.Format("{0} (Cost:{1})", m_item.Item, m_item.Cost);
            return string.Format("{0} (Cost:{1}, Limit:{2})", m_item.Item, m_item.Cost, m_item.Count);
        }
    }
}
