using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit
{
    public readonly struct NiconicoId
    {
        public readonly uint RawId;
        public readonly NiconicoContentIdType ContentIdType;

        public NiconicoId(uint id, NiconicoContentIdType contentIdType)
        {
            RawId = id;
            ContentIdType = contentIdType;
        }

        public NiconicoId(uint id)
            : this(id, NiconicoContentIdType.Unknown)
        {
        }

        public NiconicoId(int id, NiconicoContentIdType contentIdType)
        {
            if (id <= 0)
            {
                throw new InvalidOperationException("id must be positive number. id is " + id);
            }

            RawId = (uint)id;
            ContentIdType = contentIdType;
        }

        public NiconicoId(int id)
            : this(id, NiconicoContentIdType.Unknown)
        {
        }


        public NiconicoId(string id, NiconicoContentIdType contentIdType)
        {
            var (nonPrefixId, type) = IdTypeFromIdPrefix(id);
            if (type != contentIdType)
            {
                throw new ArgumentException("Difference NiconicoContentIdType between argument contentIdType and extract prefix from id.");
            }

            RawId = nonPrefixId;
            ContentIdType = type;
        }

        public NiconicoId(string idWithPrefix)
        {
            (RawId, ContentIdType) = IdTypeFromIdPrefix(idWithPrefix);
        }


        static (uint idWoPrefix, NiconicoContentIdType idType) IdTypeFromIdPrefix(string idWithPrefix)
        {
            if (idWithPrefix == null || idWithPrefix.Length == 0)
            {
                throw new ArgumentException();
            }

            if (ContentIdHelper.IsAllDigit(idWithPrefix))
            {
                return (idWithPrefix.ToUInt(), NiconicoContentIdType.Unknown);
            }

            if (idWithPrefix.Length <= 2)
            {
                return (idWithPrefix.ToUInt(), NiconicoContentIdType.Unknown);
            }

            ReadOnlySpan<char> prefix = idWithPrefix.AsSpan(0, 2);            
            if (prefix.SequenceEqual(ContentIdHelper.LiveIdPrefix.AsSpan()))
            {
                return (idWithPrefix.Skip(2).ToUInt(), NiconicoContentIdType.Live);
            }
            else if (prefix.SequenceEqual(ContentIdHelper.VideoIdPrefixForChannel.AsSpan()))
            {
                return (idWithPrefix.Skip(2).ToUInt(), NiconicoContentIdType.VideoForChannel);
            }
            else if (prefix.SequenceEqual(ContentIdHelper.VideoIdPrefixForUser.AsSpan()))
            {
                return (idWithPrefix.Skip(2).ToUInt(), NiconicoContentIdType.VideoForUser);
            }
            else if (prefix.SequenceEqual(ContentIdHelper.CommunityIdPrefix.AsSpan()))
            {
                return (idWithPrefix.Skip(2).ToUInt(), NiconicoContentIdType.Community);
            }
            else if (prefix.SequenceEqual(ContentIdHelper.ChannelIdPrefix.AsSpan()))
            {
                return (idWithPrefix.Skip(2).ToUInt(), NiconicoContentIdType.Channel);
            }
            else
            {
                return (idWithPrefix.ToUInt(), NiconicoContentIdType.Unknown);
            }

        }


        public static implicit operator int(NiconicoId id) => (int)id.RawId;
        public static implicit operator uint(NiconicoId id) => (uint)id.RawId;
        public static implicit operator string(NiconicoId id) => id.ToString();

        public static implicit operator NiconicoId(int id) => new NiconicoId(id);
        public static implicit operator NiconicoId(uint id) => new NiconicoId(id);
        public static implicit operator NiconicoId(string id) => new NiconicoId(id);


        public static bool operator ==(NiconicoId lhs, NiconicoId rhs) => lhs.Equals(rhs);
        public static bool operator !=(NiconicoId lhs, NiconicoId rhs) => !(lhs == rhs);

        public static bool operator ==(NiconicoId lhs, uint rhs) => lhs.Equals(rhs);
        public static bool operator !=(NiconicoId lhs, uint rhs) => !(lhs == rhs);
        public static bool operator ==(uint lhs, NiconicoId rhs) => rhs.Equals(lhs);
        public static bool operator !=(uint lhs, NiconicoId rhs) => !(rhs == lhs);

        public static bool operator ==(NiconicoId lhs, int rhs) => lhs.Equals(rhs);
        public static bool operator !=(NiconicoId lhs, int rhs) => !(lhs == rhs);
        public static bool operator ==(int lhs, NiconicoId rhs) => rhs.Equals(lhs);
        public static bool operator !=(int lhs, NiconicoId rhs) => !(rhs == lhs);


        public readonly override bool Equals(object obj)
        {
            return obj switch
            {
                NiconicoId id => RawId.Equals(id),

                _ => base.Equals(obj)
            };
        }

        public readonly bool Equals(NiconicoId other)
        {
            if (this.RawId == 0 || other.RawId == 0) return false;

            return this.RawId == other.RawId;
        }

        public readonly bool Equals(int other)
        {
            if (this.RawId == 0 || (uint)other == 0) return false;

            return this.RawId == (uint)other;
        }

        public readonly bool Equals(uint other)
        {
            if (this.RawId == 0 || other == 0) return false;

            return this.RawId == other;
        }

        /// <summary>
        /// Get NiconicoId with prefix.
        /// </summary>
        /// <returns>NiconicoId like "lv123456"</returns>
        public readonly string ToStringWithoutPrefix()
        {
            return RawId.ToString();
        }

        /// <summary>
        /// Get NiconicoId "without" prefix.
        /// </summary>
        /// <returns>NiconicoId like "123456"</returns>
        public readonly override string ToString()
        {
            return ContentIdType switch
            {
                NiconicoContentIdType.User => RawId.ToString(),
                NiconicoContentIdType.VideoForUser => ContentIdHelper.VideoIdPrefixForUser + RawId.ToString(),
                NiconicoContentIdType.VideoForChannel => ContentIdHelper.VideoIdPrefixForChannel + RawId.ToString(),
                NiconicoContentIdType.Live => ContentIdHelper.LiveIdPrefix + RawId.ToString(),
                NiconicoContentIdType.Community => ContentIdHelper.CommunityIdPrefix + RawId.ToString(),
                NiconicoContentIdType.Channel => ContentIdHelper.ChannelIdPrefix + RawId.ToString(),
                NiconicoContentIdType.Mylist => RawId.ToString(),
                _ => throw new NotSupportedException(ContentIdType.ToString()),
            };
        }


        /// <remarks>同種IDでのみ比較を想定して、NiconicoContentIdTypeは無視した値を生成している</remarks>
        /// <returns></returns>
        public readonly override int GetHashCode()
        {
            return RawId.GetHashCode();
        }


        public readonly bool IsUserId => ContentIdType is NiconicoContentIdType.User;
        public readonly bool IsVideoId => ContentIdType is NiconicoContentIdType.VideoForUser or NiconicoContentIdType.VideoForChannel;
        public readonly bool IsVideoIdForUser => ContentIdType is NiconicoContentIdType.VideoForUser;
        public readonly bool IsVideoIdForChannel => ContentIdType is NiconicoContentIdType.VideoForChannel;
        public readonly bool IsLiveId => ContentIdType is NiconicoContentIdType.Live;
        public readonly bool IsCommunityId => ContentIdType is NiconicoContentIdType.Community;
        public readonly bool IsChannelId => ContentIdType is NiconicoContentIdType.Channel;
        public readonly bool IsMylistId => ContentIdType is NiconicoContentIdType.Mylist;
    }


    public enum NiconicoContentIdType
    {
        Unknown,
        User,
        VideoForUser,
        VideoForChannel,
        Live,
        Community,
        Channel,
        Mylist,
    }
}
