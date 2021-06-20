using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NiconicoToolkit
{
    public static class ContentIdHelper
    {
        public static bool IsAllDigit(this IEnumerable<char> cList)
        {
            return cList.All(char.IsDigit);
        }


        public static bool TryRemoveContentIdPrefix(string inStr, out string outStr)
        {
            int index = 0;
            foreach (var c in inStr)
            {
                if (char.IsDigit(c))
                {
                    break;
                }

                index++;
            }

            if (index != 0)
            {
                if (!inStr.Skip(index).IsAllDigit())
                {
                    outStr = string.Empty;
                    return false;
                }

                outStr = inStr.Remove(0, index);
                return true;
            }
            else if (inStr.IsAllDigit())
            {
                outStr = inStr;
                return true;
            }
            else
            {
                outStr = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// sm/lv/co などのコンテンツIDの前置文字列を削除して数字のみにして返す
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string RemoveContentIdPrefix(string str)
        {
            return TryRemoveContentIdPrefix(str, out var outStr)
                ? outStr
                : throw new InvalidOperationException()
                ;
        }


        public const string VideoIdPrefixForUser = "sm";
        public const string VideoIdPrefixForChannel = "so";
        public const string VideoIdPrefixForMadeOfOfficialMovieMaker = "nm";

        internal const string VideoIdRegexBase = @"(?:sm|nm|so|ca|ax|yo|nl|ig|na|cw|z[a-e]|om|sk|yk)\d{1,14}"; // cd/fx/sd
        readonly static Regex VideoIdRegex = new Regex('^' + VideoIdRegexBase + '$');

        public static bool IsVideoId(string id, bool allowNonPrefixId = true)
        {
            if (id == null)
            {
                return false;
            }
            else if (VideoIdRegex.IsMatch(id))
            {
                return true;
            }
            else if (allowNonPrefixId && id.IsAllDigit())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsLiveId(string id, bool allowNonPrefixId = true)
        {
            if (id == null)
            {
                return false;
            }
            else if (id.StartsWith(LiveIdPrefix) && id.Skip(2).IsAllDigit())
            {
                return true;
            }
            else if (allowNonPrefixId && id.IsAllDigit())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public const string LiveIdPrefix = "lv";
        public static string EnsurePrefixLiveId(string id)
        {
            if (id.StartsWith(LiveIdPrefix))
            {
                return id;
            }
            else if (id.IsAllDigit())
            {
                return LiveIdPrefix + id;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }


        public const string CommunityIdPrefix = "co";
        public static bool IsCommunityId(string id, bool allowNonPrefixId = true)
        {
            if (id == null)
            {
                return false;
            }
            else if (id.StartsWith(CommunityIdPrefix) && id.Skip(2).IsAllDigit())
            {
                return true;
            }
            else if (allowNonPrefixId && id.IsAllDigit())
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public const string ChannelIdPrefix = "ch";
        public static bool IsChannelId(string id, bool allowNonPrefixId = true)
        {
            if (id == null)
            {
                return false;
            }
            else if (allowNonPrefixId && id.IsAllDigit())
            {
                return true;
            }
            else if (id.StartsWith(ChannelIdPrefix) && id.Skip(2).IsAllDigit())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string EnsurePrefixChannelId(string channelId)
        {
            if (channelId.StartsWith(ChannelIdPrefix) && channelId.Skip(2).IsAllDigit())
            {
                return channelId;
            }
            else if (channelId.IsAllDigit())
            {
                return ChannelIdPrefix + channelId;
            }
            else
            {
                throw new ArgumentException(nameof(channelId));
            }
        }

        public static (bool IsScreenName, string channelId) EnsurePrefixChannelIdOrScreenName(string channelId)
        {
            if (channelId.StartsWith(ChannelIdPrefix) && channelId.Skip(2).IsAllDigit())
            {
                return (false, channelId);
            }
            else if (channelId.IsAllDigit())
            {
                return (false, ChannelIdPrefix + channelId);
            }
            else
            {
                return (true, channelId);
            }
        }
    }
}
