using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch.Dmc
{
    public class ClientInfo
    {
        // res req
        [JsonPropertyName("player_id")]
        public string PlayerId { get; set; }

        // res
        [JsonPropertyName("remote_ip")]
        public string RemoteIp { get; set; }

        // res
        [JsonPropertyName("tracking_info")]
        public string TrackingInfo { get; set; }
    }
}
