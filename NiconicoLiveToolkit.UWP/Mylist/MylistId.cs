using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.Mylist
{
    public readonly struct MylistId : IEquatable<MylistId>
    {
        public readonly static MylistId WatchAfterMylistId = new MylistId(uint.MaxValue);
        public bool IsWatchAfterMylist => RawId == WatchAfterMylistId.RawId;


        public readonly uint RawId;

        public MylistId(int mylistId)
        {
            if (mylistId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(mylistId), "mylistId must be positive number.");
            }

            RawId = (uint)mylistId;
        }

        public MylistId(uint mylistId)
        {
            RawId = mylistId;
        }

        public MylistId(string mylistId)
        {
            RawId = uint.Parse(mylistId);
        }

        public static implicit operator int(MylistId mylistId) => (int)mylistId.RawId;
        public static implicit operator uint(MylistId mylistId) => (uint)mylistId.RawId;
        public static implicit operator string(MylistId mylistId) => mylistId.ToString();

        public static implicit operator MylistId(int mylistId) => new MylistId(mylistId);
        public static implicit operator MylistId(uint mylistId) => new MylistId(mylistId);
        public static implicit operator MylistId(string mylistId) => new MylistId(mylistId);

        public static implicit operator NiconicoId(MylistId mylistId) => new NiconicoId(mylistId.RawId, NiconicoIdType.Mylist);
        public static explicit operator MylistId(NiconicoId niconicoId)
        {
            if (niconicoId.IsMylistId is false)
            {
                throw new InvalidCastException();
            }

            return new MylistId(niconicoId.RawId);
        }
        public static bool operator ==(MylistId lhs, MylistId rhs) => lhs.Equals(rhs);
        public static bool operator !=(MylistId lhs, MylistId rhs) => !(lhs == rhs);

        public static bool operator ==(MylistId lhs, uint rhs) => lhs.Equals(rhs);
        public static bool operator !=(MylistId lhs, uint rhs) => !(lhs == rhs);
        public static bool operator ==(uint lhs, MylistId rhs) => rhs.Equals(lhs);
        public static bool operator !=(uint lhs, MylistId rhs) => !(rhs == lhs);

        public static bool operator ==(MylistId lhs, int rhs) => lhs.Equals(rhs);
        public static bool operator !=(MylistId lhs, int rhs) => !(lhs == rhs);
        public static bool operator ==(int lhs, MylistId rhs) => rhs.Equals(lhs);
        public static bool operator !=(int lhs, MylistId rhs) => !(rhs == lhs);


        public override bool Equals(object obj)
        {
            return obj switch
            {
                MylistId id => RawId.Equals(id),

                _ => base.Equals(obj)
            };
        }

        public bool Equals(MylistId other)
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
