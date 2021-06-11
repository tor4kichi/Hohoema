using System.Text.Json.Serialization;
#if WINDOWS_UWP
#else
using System.Net;
using System.Net.Http;
#endif


namespace NiconicoToolkit.Mylist.LoginUser
{
    public sealed class ChangeMylistGroupsOrderResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public ChangeMylistGroupsOrderData Data { get; set; }


        public sealed class ChangeMylistGroupsOrderData
        {
            [JsonPropertyName("mylistIds")]
            public string[] MylistIds { get; set; }
        }
    }
}
