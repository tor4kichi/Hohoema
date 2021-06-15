using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.User
{
    public readonly struct UserId : IEquatable<UserId>
    {
        private readonly uint _userId;

        public UserId(int userId)
        {
            if (userId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), "userId must be positive number.");
            }

            _userId = (uint)userId;
        }

        public UserId(uint userId)
        {
            _userId = userId;
        }

        public UserId(string userId)
        {
            _userId = uint.Parse(userId);
        }

        public static implicit operator int(UserId userId) => (int)userId._userId;
        public static implicit operator uint(UserId userId) => (uint)userId._userId;
        public static implicit operator string(UserId userId) => userId.ToString();

        public static implicit operator UserId(int userId) => new UserId(userId);
        public static implicit operator UserId(uint userId) => new UserId(userId);
        public static implicit operator UserId(string userId) => new UserId(userId);


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
                UserId id => _userId.Equals(id),

                _ => base.Equals(obj)
            };            
        }

        public bool Equals(UserId other)
        {
            if (this._userId == 0 || other._userId == 0) return false;

            return this._userId == other._userId;
        }

        public bool Equals(int other)
        {
            if (this._userId == 0 || (uint)other == 0) return false;

            return this._userId == (uint)other;
        }

        public bool Equals(uint other)
        {
            if (this._userId == 0 || other == 0) return false;

            return this._userId == other;
        }





        public override string ToString()
        {
            return _userId.ToString();
        }


        public override int GetHashCode()
        {
            return _userId.GetHashCode();
        }

    }
}
