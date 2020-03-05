using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Ceebeetle
{
    [DataContract(Name = "P2PStore")]
    class CCBP2PStore
    {
        [DataMember(Name = "StoreID")]
        private Guid m_id;
        [DataMember(Name = "StoreOwner")]
        private string m_owner;
        [DataMember(Name = "ActualStore")]
        private CCBStore m_store;

        public Guid ID
        {
            get { return m_id; }
        }
        public string Owner
        {
            get { return m_owner; }
            set { m_owner = value; }
        }

        public CCBP2PStore(string owner, CCBStore store)
        {
            m_id = Guid.NewGuid();
            m_owner = owner;
            m_store = store;
        }
    }
}
