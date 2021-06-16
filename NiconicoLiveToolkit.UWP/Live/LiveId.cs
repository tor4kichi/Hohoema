using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.Live
{
    public readonly struct LiveId 
    {
        public readonly uint RawId;

        public LiveId(int id)
        {
            if (id <= 0)
            {
                throw new InvalidOperationException("id must be positive number. id is " + id);
            }

            RawId = (uint)id;
        }

        public LiveId(uint id)
        {
            RawId = id;
        }

        public LiveId(string id)
        {
            if (ContentIdHelper.IsLiveId(id, allowNonPrefixId: false))
            {
                RawId = uint.Parse(id.Remove(0, 2));
            }
            else
            {
                RawId = uint.Parse(id);
            }
        }


        public static implicit operator int(LiveId liveId) => (int)liveId.RawId;
        public static implicit operator uint(LiveId liveId) => (uint)liveId.RawId;
        public static implicit operator string(LiveId liveId) => liveId.ToString();

        public static implicit operator LiveId(int liveId) => new LiveId(liveId);
        public static implicit operator LiveId(uint liveId) => new LiveId(liveId);
        public static implicit operator LiveId(string liveId) => new LiveId(liveId);

        public static implicit operator NiconicoId(LiveId liveId) => new NiconicoId(liveId.RawId, NiconicoIdType.Live);
        public static explicit operator LiveId(NiconicoId niconicoId)
        {
            if (niconicoId.IsLiveId is false)
            {
                throw new InvalidCastException();
            }

            return new LiveId(niconicoId.RawId);
        }



        public static bool operator ==(LiveId lhs, LiveId rhs) => lhs.Equals(rhs);
        public static bool operator !=(LiveId lhs, LiveId rhs) => !(lhs == rhs);

        public static bool operator ==(LiveId lhs, uint rhs) => lhs.Equals(rhs);
        public static bool operator !=(LiveId lhs, uint rhs) => !(lhs == rhs);
        public static bool operator ==(uint lhs, LiveId rhs) => rhs.Equals(lhs);
        public static bool operator !=(uint lhs, LiveId rhs) => !(rhs == lhs);

        public static bool operator ==(LiveId lhs, int rhs) => lhs.Equals(rhs);
        public static bool operator !=(LiveId lhs, int rhs) => !(lhs == rhs);
        public static bool operator ==(int lhs, LiveId rhs) => rhs.Equals(lhs);
        public static bool operator !=(int lhs, LiveId rhs) => !(rhs == lhs);


        public override bool Equals(object obj)
        {
            return obj switch
            {
                LiveId id => RawId.Equals(id),

                _ => base.Equals(obj)
            };
        }

        public bool Equals(LiveId other)
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
        /// Get LiveId with prefix.
        /// </summary>
        /// <returns>LiveId like "lv123456"</returns>
        public override string ToString()
        {
            return ContentIdHelper.LiveIdPrefix + RawId;
        }

        /// <summary>
        /// Get LiveId "without" prefix.
        /// </summary>
        /// <returns>LiveId like "123456"</returns>
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
