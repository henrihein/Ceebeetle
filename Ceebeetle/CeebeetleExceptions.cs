using System;

namespace Ceebeetle
{
    public class CEStoreManagerNoPlaceFound : System.Exception
    {
        public CEStoreManagerNoPlaceFound(string errMsg, int ix)
            : base(string.Format(errMsg, ix))
        {
        }
    }
}
