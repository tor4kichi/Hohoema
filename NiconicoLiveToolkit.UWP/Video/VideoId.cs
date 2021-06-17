using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video
{
    public enum VideoIdType
    {
        VideoForUser,
        VideoForChannel,
        VideoAlias,
    }
    public readonly struct VideoId : IEquatable<VideoId>
    {
        public readonly uint RawId;
        public readonly VideoIdType IdType;
        public readonly string StrId;


        private VideoId(int id, VideoIdType idType)
        {
            if (id <= 0)
            {
                throw new InvalidOperationException("id must be positive number. id is " + id);
            }

            RawId = (uint)id;
            IdType = idType;
            StrId = ToPrefixId(RawId, IdType);
        }

        private VideoId(uint id, VideoIdType idType)
        {
            RawId = id;
            IdType = idType;
            StrId = ToPrefixId(RawId, IdType);
        }

        public VideoId(int id)
            : this(id, VideoIdType.VideoAlias)
        {
        }

        public VideoId(uint id)
            : this(id, VideoIdType.VideoAlias)
        {
        }

        private VideoId(string id, VideoIdType idType)
        {
            var (number, realIdType) = ExtractIdNumberAndIdType(id);
            if (realIdType != idType)
            {
                throw new InvalidOperationException();
            }

            RawId = number;
            IdType = idType;
            StrId = id;
        }

        public VideoId(string id)
        {
            (RawId, IdType) = ExtractIdNumberAndIdType(id);
            StrId = id;
        }


        public static (uint number, VideoIdType idType) ExtractIdNumberAndIdType(string idWithPrefix)
        {
            if (idWithPrefix == null || idWithPrefix.Length == 0)
            {
                throw new ArgumentException();
            }

            if (ContentIdHelper.IsAllDigit(idWithPrefix))
            {
                return (idWithPrefix.ToUInt(), VideoIdType.VideoAlias);
            }

            if (idWithPrefix.Length <= 2)
            {
                return (idWithPrefix.ToUInt(), VideoIdType.VideoAlias);
            }

            ReadOnlySpan<char> prefix = idWithPrefix.AsSpan(0, 2);
            if (prefix.SequenceEqual(ContentIdHelper.VideoIdPrefixForUser.AsSpan()))
            {
                return (idWithPrefix.Skip(2).ToUInt(), VideoIdType.VideoForUser);
            }
            else if (prefix.SequenceEqual(ContentIdHelper.VideoIdPrefixForChannel.AsSpan()))
            {
                return (idWithPrefix.Skip(2).ToUInt(), VideoIdType.VideoForChannel);
            }
            else
            {
                throw new NotSupportedException(idWithPrefix);
            }
        }

        public static implicit operator int(VideoId videoId) => (int)videoId.RawId;
        public static implicit operator uint(VideoId videoId) => (uint)videoId.RawId;
        public static implicit operator string(VideoId videoId) => videoId.ToString();

        public static implicit operator VideoId(int videoId) => new VideoId(videoId, VideoIdType.VideoAlias);
        public static implicit operator VideoId(uint videoId) => new VideoId(videoId, VideoIdType.VideoAlias);
        public static implicit operator VideoId(string videoId) => new VideoId(videoId);

        public static implicit operator NiconicoId(VideoId videoId) => new NiconicoId(videoId.RawId, FromVideoIdType(videoId.IdType));
        public static explicit operator VideoId(NiconicoId niconicoId)
        {
            if (niconicoId.IsVideoId is false)
            {
                throw new InvalidCastException();
            }

            return new VideoId(niconicoId.RawId, ToVideoIdType(niconicoId.IdType));
        }


        public static VideoIdType ToVideoIdType(NiconicoIdType idType)
        {
            return idType switch
            {
                NiconicoIdType.VideoForUser => VideoIdType.VideoForUser,
                NiconicoIdType.VideoForChannel => VideoIdType.VideoForChannel,
                NiconicoIdType.VideoAlias => VideoIdType.VideoAlias,
                NiconicoIdType.Unknown => VideoIdType.VideoAlias,
                _ => throw new InvalidOperationException($"can not convert to {nameof(VideoIdType)} from {nameof(NiconicoIdType)} ({idType})"),
            };
        }

        public static NiconicoIdType FromVideoIdType(VideoIdType idType)
        {
            return idType switch
            {
                VideoIdType.VideoForUser => NiconicoIdType.VideoForUser,
                VideoIdType.VideoForChannel => NiconicoIdType.VideoForChannel,
                VideoIdType.VideoAlias => NiconicoIdType.VideoAlias,
                _ => throw new InvalidOperationException(),
            };
        }


        public static bool operator ==(VideoId lhs, VideoId rhs) => lhs.Equals(rhs);
        public static bool operator !=(VideoId lhs, VideoId rhs) => !(lhs == rhs);

        public static bool operator ==(VideoId lhs, uint rhs) => lhs.Equals(rhs);
        public static bool operator !=(VideoId lhs, uint rhs) => !(lhs == rhs);
        public static bool operator ==(uint lhs, VideoId rhs) => rhs.Equals(lhs);
        public static bool operator !=(uint lhs, VideoId rhs) => !(rhs == lhs);

        public static bool operator ==(VideoId lhs, int rhs) => lhs.Equals(rhs);
        public static bool operator !=(VideoId lhs, int rhs) => !(lhs == rhs);
        public static bool operator ==(int lhs, VideoId rhs) => rhs.Equals(lhs);
        public static bool operator !=(int lhs, VideoId rhs) => !(rhs == lhs);


        public override bool Equals(object obj)
        {
            return obj switch
            {
                VideoId id => RawId.Equals(id),

                _ => base.Equals(obj)
            };
        }

        public bool Equals(VideoId other)
        {
            if (this.RawId == 0 || other.RawId == 0) return false;

            return this.RawId == other.RawId;
        }

        public bool Equals(int other)
        {
            if (this.RawId == 0 || (uint)other == 0) return false;

            return this.RawId == (uint)other;
        }

        public bool Equals(uint other)
        {
            if (this.RawId == 0 || other == 0) return false;

            return this.RawId == other;
        }

        /// <summary>
        /// Get VideoId with prefix.
        /// </summary>
        /// <returns>VideoId like "lv123456"</returns>
        public override string ToString()
        {
            return StrId;
        }

        public static string ToPrefixId(uint rawId, VideoIdType idType)
        {
            return idType switch
            {
                VideoIdType.VideoForUser => ContentIdHelper.VideoIdPrefixForUser + rawId,
                VideoIdType.VideoForChannel => ContentIdHelper.VideoIdPrefixForChannel + rawId,
                VideoIdType.VideoAlias => rawId.ToString(),
                _ => throw new InvalidOperationException(),
            };
        }

        public override int GetHashCode()
        {
            return StrId.GetHashCode();
        }

    }
}
