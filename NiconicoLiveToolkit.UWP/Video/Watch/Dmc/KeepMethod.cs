using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch.Dmc
{
    public class KeepMethod
    {

        [JsonPropertyName("heartbeat")]
        public Heartbeat Heartbeat { get; set; }
    }

    public class Heartbeat
    {

        [JsonPropertyName("lifetime")]
        public int Lifetime { get; set; }

        [JsonPropertyName("onetime_token")]
        public string OnetimeToken { get; set; }
    }
}
