using System;

namespace NiconicoToolkit.Live.Timeshift
{
    public sealed class ReservationToken : IEquatable<ReservationToken>
    {
        public static readonly ReservationToken InavalidToken = new ReservationToken();

        private ReservationToken() { }

        public ReservationToken(string token)
        {
            Token = token;
        }
        public string Token { get; }

        public bool Equals(ReservationToken other)
        {
            return Token == other?.Token;
        }
    }
}
