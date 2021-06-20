using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.Community
{
    
    public readonly struct CommunityId
    {
        public readonly uint RawId;

        public CommunityId(int id)
        {
            if (id <= 0)
            {
                throw new InvalidOperationException("id must be positive number. id is " + id);
            }

            RawId = (uint)id;
        }

        public CommunityId(uint id)
        {
            RawId = id;
        }

        public CommunityId(string id)
        {
            if (ContentIdHelper.IsCommunityId(id, allowNonPrefixId: false))
            {
                RawId = uint.Parse(id.Remove(0, 2));
            }
            else
            {
                RawId = uint.Parse(id);
            }
        }


        public static implicit operator int(CommunityId communityId) => (int)communityId.RawId;
        public static implicit operator uint(CommunityId communityId) => communityId.RawId;
        public static implicit operator string(CommunityId communityId) => communityId.ToString();

        public static implicit operator CommunityId(int communityId) => new CommunityId(communityId);
        public static implicit operator CommunityId(uint communityId) => new CommunityId(communityId);
        public static implicit operator CommunityId(string communityId) => new CommunityId(communityId);

        public static implicit operator NiconicoId(CommunityId communityId) => new NiconicoId(communityId.RawId, NiconicoIdType.Community);
        public static explicit operator CommunityId(NiconicoId niconicoId)
        {
            if (niconicoId.IsCommunityId is false)
            {
                throw new InvalidCastException();
            }

            return new CommunityId(niconicoId.RawId);
        }



        public static bool operator ==(CommunityId lhs, CommunityId rhs) => lhs.Equals(rhs);
        public static bool operator !=(CommunityId lhs, CommunityId rhs) => !(lhs == rhs);

        public static bool operator ==(CommunityId lhs, uint rhs) => lhs.Equals(rhs);
        public static bool operator !=(CommunityId lhs, uint rhs) => !(lhs == rhs);
        public static bool operator ==(uint lhs, CommunityId rhs) => rhs.Equals(lhs);
        public static bool operator !=(uint lhs, CommunityId rhs) => !(rhs == lhs);

        public static bool operator ==(CommunityId lhs, int rhs) => lhs.Equals(rhs);
        public static bool operator !=(CommunityId lhs, int rhs) => !(lhs == rhs);
        public static bool operator ==(int lhs, CommunityId rhs) => rhs.Equals(lhs);
        public static bool operator !=(int lhs, CommunityId rhs) => !(rhs == lhs);


        public override bool Equals(object obj)
        {
            return obj switch
            {
                CommunityId id => RawId.Equals(id),

                _ => base.Equals(obj)
            };
        }

        public bool Equals(CommunityId other)
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
        /// Get CommunityId with prefix.
        /// </summary>
        /// <returns>CommunityId like "lv123456"</returns>
        public override string ToString()
        {
            return ContentIdHelper.CommunityIdPrefix + RawId;
        }

        /// <summary>
        /// Get CommunityId "without" prefix.
        /// </summary>
        /// <returns>CommunityId like "123456"</returns>
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
