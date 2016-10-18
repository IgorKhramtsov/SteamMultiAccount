using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamMultiAccount
{
    public class Key
    {
        internal string key;
        internal uint appID;
        internal string gameName;
        internal CustomHandler.PurchaseResponseCallback.EPurchaseResult ActivatingResult;
        internal string botName;
        public Key(string _key)
        {
            key = _key;
        }
        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(botName))
                return $"{key} ({ActivatingResult}) - {botName}"; // AAAAA-BBBBB-CCCCC (result) - Bot name
            else if (ActivatingResult != CustomHandler.PurchaseResponseCallback.EPurchaseResult.Unknown)
                return $"{key} ({ActivatingResult})"; // AAAAA-BBBBB-CCCCC (result)
            else
                return key;
        }
        public override bool Equals(object obj)
        {
            if (obj is Key)
                return (obj as Key).key == this.key;

            return base.Equals(obj);
        }
    }
}
