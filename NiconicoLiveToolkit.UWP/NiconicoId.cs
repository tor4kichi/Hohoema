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
        public readonly NiconicoIdType IdType;

        public NiconicoId(uint id, NiconicoIdType idType)
        {
            RawId = id;
            IdType = idType;
        }

        public NiconicoId(uint id)
            : this(id, NiconicoIdType.Unknown)
        {
        }

        public NiconicoId(int id, NiconicoIdType idType)
        {
            if (id <= 0)
            {
                throw new InvalidOperationException("id must be positive number. id is " + id);
            }

            RawId = (uint)id;
            IdType = idType;
        }

        public NiconicoId(int id)
            : this(id, NiconicoIdType.Unknown)
        {
        }


        public NiconicoId(string id, NiconicoIdType idType)
        {
            var (nonPrefixId, type) = IdTypeFromIdPrefix(id);
            if (type != NiconicoIdType.Unknown && type != idType)
            {
                throw new ArgumentException("Difference NiconicoContentIdType between argument idType and extract prefix from id.");
            }

            RawId = nonPrefixId;
            IdType = type;
        }

        public NiconicoId(string idWithPrefix)
        {
            (RawId, IdType) = IdTypeFromIdPrefix(idWithPrefix);
        }


        static (uint idWoPrefix, NiconicoIdType idType) IdTypeFromIdPrefix(string idWithPrefix)
        {
            if (idWithPrefix == null || idWithPrefix.Length == 0)
            {
                throw new ArgumentException();
            }

            if (ContentIdHelper.IsAllDigit(idWithPrefix))
            {
                return (idWithPrefix.ToUInt(), NiconicoIdType.Unknown);
            }

            if (idWithPrefix.Length <= 2)
            {
                return (idWithPrefix.ToUInt(), NiconicoIdType.Unknown);
            }

            ReadOnlySpan<char> prefix = idWithPrefix.AsSpan(0, 2);            
            
            if (prefix.SequenceEqual(ContentIdHelper.VideoIdPrefixForUser.AsSpan()))
            {
                return (idWithPrefix.Skip(2).ToUInt(), NiconicoIdType.VideoForUser);
            }
            else if (prefix.SequenceEqual(ContentIdHelper.VideoIdPrefixForChannel.AsSpan()))
            {
                return (idWithPrefix.Skip(2).ToUInt(), NiconicoIdType.VideoForChannel);
            }
            else if (prefix.SequenceEqual(ContentIdHelper.LiveIdPrefix.AsSpan()))
            {
                return (idWithPrefix.Skip(2).ToUInt(), NiconicoIdType.Live);
            }
            else if (prefix.SequenceEqual(ContentIdHelper.CommunityIdPrefix.AsSpan()))
            {
                return (idWithPrefix.Skip(2).ToUInt(), NiconicoIdType.Community);
            }
            else if (prefix.SequenceEqual(ContentIdHelper.ChannelIdPrefix.AsSpan()))
            {
                return (idWithPrefix.Skip(2).ToUInt(), NiconicoIdType.Channel);
            }
            else
            {
                return (idWithPrefix.ToUInt(), NiconicoIdType.Unknown);
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
            return IdType switch
            {
                NiconicoIdType.User => RawId.ToString(),
                NiconicoIdType.VideoForUser => ContentIdHelper.VideoIdPrefixForUser + RawId.ToString(),
                NiconicoIdType.VideoForChannel => ContentIdHelper.VideoIdPrefixForChannel + RawId.ToString(),
                NiconicoIdType.VideoAlias => RawId.ToString(),
                NiconicoIdType.Live => ContentIdHelper.LiveIdPrefix + RawId.ToString(),
                NiconicoIdType.Community => ContentIdHelper.CommunityIdPrefix + RawId.ToString(),
                NiconicoIdType.Channel => ContentIdHelper.ChannelIdPrefix + RawId.ToString(),
                NiconicoIdType.Mylist => RawId.ToString(),
                _ => RawId.ToString(),
            };
        }


        /// <remarks>同種IDでのみ比較を想定して、NiconicoContentIdTypeは無視した値を生成している</remarks>
        /// <returns></returns>
        public readonly override int GetHashCode()
        {
            return RawId.GetHashCode();
        }


        public readonly bool IsUserId => IdType is NiconicoIdType.User;
        public readonly bool IsVideoId => IdType is NiconicoIdType.VideoForUser or NiconicoIdType.VideoForChannel or NiconicoIdType.VideoAlias;
        public readonly bool IsVideoIdForUser => IdType is NiconicoIdType.VideoForUser;
        public readonly bool IsVideoIdForChannel => IdType is NiconicoIdType.VideoForChannel;
        public readonly bool IsLiveId => IdType is NiconicoIdType.Live;
        public readonly bool IsCommunityId => IdType is NiconicoIdType.Community;
        public readonly bool IsChannelId => IdType is NiconicoIdType.Channel;
        public readonly bool IsMylistId => IdType is NiconicoIdType.Mylist;
    }


    public enum NiconicoIdType
    {
        Unknown,
        User,
        VideoForUser,
        VideoForChannel,
        VideoAlias,
        Live,
        Community,
        Channel,
        Mylist,
    }
}
