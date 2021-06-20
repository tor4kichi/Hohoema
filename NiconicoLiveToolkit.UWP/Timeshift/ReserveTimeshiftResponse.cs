using System.Text.Json.Serialization;


namespace NiconicoToolkit.Live.Timeshift
{
    public sealed class ReserveTimeshiftResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public ReserveTimeshiftData Data { get; private set; }


        public bool IsReservationDeuplicated => Data.Description?.EndsWith("duplicated") ?? false;

        public bool IsReservationExpired => Data.Description?.EndsWith("expired general") ?? false;

        public bool IsCanOverwrite => Data.Description?.EndsWith("can overwrite") ?? false;


        public ReservationDescription? GetReservationDescription()
        {
            return Data.Description switch
            {
                null => null,
                string r when r.EndsWith("duplicated") => ReservationDescription.Deuplicated,
                string r when r.EndsWith("expired general") => ReservationDescription.Expired,
                string r when r.EndsWith("can overwrite") => ReservationDescription.CanOverwrite,
                _ => null,
            };
        }
    }



    public enum ReservationDescription
    {
        Deuplicated,
        Expired,
        CanOverwrite,
    }

    public sealed class ReserveTimeshiftData
    {
        [JsonPropertyName("description")]
        public string Description { get; private set; }

        [JsonPropertyName("uid")]
        public string Uid { get; private set; }

        [JsonPropertyName("vid")]
        public LiveId Vid { get; private set; }

        [JsonPropertyName("overwrite")]
        public ReserveTimeshiftOverwrite Overwrite { get; private set; }
    }


    public sealed class ReserveTimeshiftOverwrite
    {
        [JsonPropertyName("vid")]
        public LiveId Vid { get; private set; }

        [JsonPropertyName("title")]
        public string Title { get; private set; }
    }

}
