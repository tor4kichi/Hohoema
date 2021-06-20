using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.User
{
    /// <summary>
    /// 数字のみで構成されたユーザーIDを扱う。
    /// コメント向けの匿名ユーザー（ランダム文字列）は対応していない。
    /// </summary>
    public readonly struct UserId : IEquatable<UserId>
    {
        public static readonly UserId IgnoreUserId = new UserId(0);
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
            RawId = uint.TryParse(userId, out var id) ? id : IgnoreUserId.RawId;
        }

        public static implicit operator int(UserId userId) => (int)userId.RawId;
        public static implicit operator uint(UserId userId) => (uint)userId.RawId;
        public static implicit operator string(UserId userId) => userId.ToString();

        public static implicit operator UserId(int userId) => new UserId(userId);
        public static implicit operator UserId(uint userId) => new UserId(userId);
        public static implicit operator UserId(string userId) => userId != null ? new UserId(userId) : IgnoreUserId;

        public static implicit operator NiconicoId(UserId userId) => new NiconicoId(userId.RawId, NiconicoIdType.User);
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

        public static bool operator ==(UserId? lhs, UserId rhs) => lhs is not null ? lhs.Value.Equals(rhs) : false;
        public static bool operator !=(UserId? lhs, UserId rhs) => !(lhs == rhs);

        public static bool operator ==(UserId lhs, UserId? rhs) => rhs is not null ? lhs.Equals(rhs.Value) : false;
        public static bool operator !=(UserId lhs, UserId? rhs) => !(lhs == rhs);

        public static bool operator ==(UserId? lhs, UserId? rhs)
        {
            if (!lhs.HasValue && !rhs.HasValue) { return true; }
            else if (lhs.HasValue && rhs.HasValue) { return lhs.Value == rhs.Value; }
            else if (lhs.HasValue) { return lhs.Value == rhs; }
            else { return lhs == rhs.Value; }
        }

        public static bool operator !=(UserId? lhs, UserId? rhs) => !(lhs == rhs);

        public static bool operator ==(UserId lhs, uint rhs) => lhs.Equals(rhs);
        public static bool operator !=(UserId lhs, uint rhs) => !(lhs == rhs);
        public static bool operator ==(uint lhs, UserId rhs) => rhs.Equals(lhs);
        public static bool operator !=(uint lhs, UserId rhs) => !(rhs == lhs);

        public static bool operator ==(UserId lhs, int rhs) => lhs.Equals(rhs);
        public static bool operator !=(UserId lhs, int rhs) => !(lhs == rhs);
        public static bool operator ==(int lhs, UserId rhs) => rhs.Equals(lhs);
        public static bool operator !=(int lhs, UserId rhs) => !(rhs == lhs);

        public static bool operator ==(UserId lhs, string rhs) => lhs.Equals(rhs);
        public static bool operator !=(UserId lhs, string rhs) => !(lhs == rhs);
        public static bool operator ==(string lhs, UserId rhs) => rhs.Equals(lhs);
        public static bool operator !=(string lhs, UserId rhs) => !(rhs == lhs);

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
            if (this == IgnoreUserId.RawId || other.RawId == IgnoreUserId.RawId) return false;

            return this.RawId == other.RawId;
        }

        public bool Equals(int other)
        {
            if (this == IgnoreUserId.RawId || (uint)other == IgnoreUserId.RawId) return false;

            return this.RawId == (uint)other;
        }

        public bool Equals(uint other)
        {
            if (this.RawId == IgnoreUserId.RawId || other == IgnoreUserId.RawId) return false;

            return this.RawId == other;
        }

        public bool Equals(string other)
        {
            if (other == null) { return false; }
            if (this.RawId == IgnoreUserId.RawId) { return false; }

            var otherUserId = new UserId(other);
            if (otherUserId.RawId == IgnoreUserId.RawId) return false;

            return this.RawId == otherUserId.RawId;
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
