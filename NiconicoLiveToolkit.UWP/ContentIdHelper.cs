using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit
{
    public static class ContentIdHelper
    {
        public static bool IsAllDigit(this IEnumerable<char> cList)
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

        public const string CommunityIdPrefix = "co";
        public static bool IsCommunityId(string id, bool allowNumberOnlyId = true)
        {
            if (id.StartsWith(CommunityIdPrefix) && id.Skip(2).IsAllDigit())
            {
                return true;
            }
            else if (allowNumberOnlyId && id.IsAllDigit())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string EnsureNonPrefixCommunityId(string id)
        {
            if (id.IsAllDigit())
            {
                return id;
            }
            else if (IsCommunityId(id, false))
            {
                return id.Remove(0, 2);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }


        public static bool IsChannelId(string id, bool allowNumberOnlyId = true)
        {
            if (allowNumberOnlyId && id.IsAllDigit())
            {
                return true;
            }
            else if (id.StartsWith("ch") && id.Skip(2).IsAllDigit())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string EnsureNonPrefixChannelId(string id)
        {
            if (id.IsAllDigit())
            {
                return id;
            }
            else if (IsChannelId(id, allowNumberOnlyId: false))
            {
                return id.Remove(0, 2);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static (bool IsScreenName, string channelId) EnsurePrefixChannelIdOrScreenName(string channelId)
        {
            if (channelId.StartsWith("ch") && channelId.Skip(2).IsAllDigit())
            {
                return (false, channelId);
            }
            else if (channelId.IsAllDigit())
            {
                return (false, "ch" + channelId);
            }
            else
            {
                return (true, channelId);
            }
        }
    }
}
