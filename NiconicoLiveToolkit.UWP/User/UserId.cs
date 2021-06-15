using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.User
{
    public readonly struct UserId : IEquatable<UserId>
    {
        public readonly uint RawId;

        public UserId(int userId)
        {
            if (userId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), "userId must be positive number.");
            }

            RawId = (uint)userId;
        }

        public UserId(uint userId)
        {
            RawId = userId;
        }

        public UserId(string userId)
        {
            RawId = uint.Parse(userId);
        }

        public static implicit operator int(UserId userId) => (int)userId.RawId;
        public static implicit operator uint(UserId userId) => (uint)userId.RawId;
        public static implicit operator string(UserId userId) => userId.ToString();

        public static implicit operator UserId(int userId) => new UserId(userId);
        public static implicit operator UserId(uint userId) => new UserId(userId);
        public static implicit operator UserId(string userId) => new UserId(userId);

        public static implicit operator NiconicoId(UserId userId) => new NiconicoId(userId.RawId, NiconicoContentIdType.User);
        public static explicit operator UserId(NiconicoId niconicoId)
        {
            if (niconicoId.IsUserId is false)
            {
                throw new InvalidCastException();
            }

            return new UserId(niconicoId.RawId);
        }
        public static bool operator ==(UserId lhs, UserId rhs) => lhs.Equals(rhs);
        public static bool operator !=(UserId lhs, UserId rhs) => !(lhs == rhs);

        public static bool operator ==(UserId lhs, uint rhs) => lhs.Equals(rhs);
        public static bool operator !=(UserId lhs, uint rhs) => !(lhs == rhs);
        public static bool operator ==(uint lhs, UserId rhs) => rhs.Equals(lhs);
        public static bool operator !=(uint lhs, UserId rhs) => !(rhs == lhs);

        public static bool operator ==(UserId lhs, int rhs) => lhs.Equals(rhs);
        public static bool operator !=(UserId lhs, int rhs) => !(lhs == rhs);
        public static bool operator ==(int lhs, UserId rhs) => rhs.Equals(lhs);
        public static bool operator !=(int lhs, UserId rhs) => !(rhs == lhs);


        public override bool Equals(object obj)
        {
            return obj switch
            {
                UserId id => RawId.Equals(id),

                _ => base.Equals(obj)
            };            
        }

        public bool Equals(UserId other)
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





        public override string ToString()
        {
            return RawId.ToString();
        }


        public override int GetHashCode()
        {
            return RawId.GetHashCode();
        }

    }
}
