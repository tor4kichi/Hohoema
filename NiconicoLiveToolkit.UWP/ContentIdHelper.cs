using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit
{
    public static class ContentIdHelper
    {
        private static bool IsAllDigit(this IEnumerable<char> cList)
        {
            return cList.All(char.IsDigit);
        }

        public static bool IsVideoId(string id, bool allowAllNumberId = true)
        {
            if (id.StartsWith("sm") && id.Skip(2).IsAllDigit())
            {
                return true;
            }
            else if (id.StartsWith("so") && id.Skip(2).IsAllDigit())
            {
                return true;
            }
            else if (allowAllNumberId && id.IsAllDigit())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsLiveId(string id)
        {
            if (id.StartsWith("lv") && id.Skip(2).IsAllDigit())
            {
                return true;
            }
            else
            {
                return false;
            }
        }


    }
}
