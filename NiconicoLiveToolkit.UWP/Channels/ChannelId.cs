using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.Channels
{
    public readonly struct ChannelId
    {
        public readonly uint RawId;

        public ChannelId(int id)
        {
            if (id <= 0)
            {
                throw new InvalidOperationException("id must be positive number. id is " + id);
            }

            RawId = (uint)id;
        }

        public ChannelId(uint id)
        {
            RawId = id;
        }

        public ChannelId(string id)
        {
            if (ContentIdHelper.IsChannelId(id, allowNonPrefixId: false))
            {
                RawId = uint.Parse(id.Remove(0, 2));
            }
            else
            {
                RawId = uint.Parse(id);
            }
        }


        public static implicit operator int(ChannelId channelId) => (int)channelId.RawId;
        public static implicit operator uint(ChannelId channelId) => channelId.RawId;
        public static implicit operator string(ChannelId channelId) => channelId.ToString();

        public static implicit operator ChannelId(int channelId) => new ChannelId(channelId);
        public static implicit operator ChannelId(uint channelId) => new ChannelId(channelId);
        public static implicit operator ChannelId(string channelId) => new ChannelId(channelId);

        public static implicit operator NiconicoId(ChannelId channelId) => new NiconicoId(channelId.RawId, NiconicoIdType.Channel);
        public static explicit operator ChannelId(NiconicoId niconicoId)
        {
            if (niconicoId.IsChannelId is false)
            {
                throw new InvalidCastException();
            }

            return new ChannelId(niconicoId.RawId);
        }



        public static bool operator ==(ChannelId lhs, ChannelId rhs) => lhs.Equals(rhs);
        public static bool operator !=(ChannelId lhs, ChannelId rhs) => !(lhs == rhs);

        public static bool operator ==(ChannelId lhs, uint rhs) => lhs.Equals(rhs);
        public static bool operator !=(ChannelId lhs, uint rhs) => !(lhs == rhs);
        public static bool operator ==(uint lhs, ChannelId rhs) => rhs.Equals(lhs);
        public static bool operator !=(uint lhs, ChannelId rhs) => !(rhs == lhs);

        public static bool operator ==(ChannelId lhs, int rhs) => lhs.Equals(rhs);
        public static bool operator !=(ChannelId lhs, int rhs) => !(lhs == rhs);
        public static bool operator ==(int lhs, ChannelId rhs) => rhs.Equals(lhs);
        public static bool operator !=(int lhs, ChannelId rhs) => !(rhs == lhs);


        public override bool Equals(object obj)
        {
            return obj switch
            {
                ChannelId id => RawId.Equals(id),

                _ => base.Equals(obj)
            };
        }

        public bool Equals(ChannelId other)
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
        /// Get ChannelId with prefix.
        /// </summary>
        /// <returns>ChannelId like "lv123456"</returns>
        public override string ToString()
        {
            return ContentIdHelper.ChannelIdPrefix + RawId;
        }

        /// <summary>
        /// Get ChannelId "without" prefix.
        /// </summary>
        /// <returns>ChannelId like "123456"</returns>
        public string ToStringWithoutPrefix()
        {
            return RawId.ToString();
        }


        public override int GetHashCode()
        {
            return RawId.GetHashCode();
        }

    }
}
