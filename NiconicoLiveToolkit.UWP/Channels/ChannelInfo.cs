using System;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Channels
{
    public sealed class ChannelInfo
    {

        [JsonPropertyName("channel_id")]
        public ChannelId ChannelId { get; set; }

        [JsonPropertyName("category_id")]
        public int CategoryId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("company_viewname")]
        public string CompanyViewname { get; set; }

        [JsonPropertyName("open_time")]
        public string OpenTime { get; set; }

        [JsonPropertyName("update_time")]
        public string UpdateTime { get; set; }

        //[JsonPropertyName("dfp_setting")]
        //public string DfpSetting { get; set; }

        [JsonPropertyName("screen_name")]
        public string ScreenName { get; set; }


        public DateTime ParseOpenTime()
        {
            return DateTime.Parse(OpenTime);
        }

        public DateTime ParseUpdateTime()
        {
            return DateTime.Parse(UpdateTime);
        }
    }
}
