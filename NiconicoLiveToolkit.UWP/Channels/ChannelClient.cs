using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Channels
{
    public sealed class ChannelClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _options;

        public ChannelClient(NiconicoContext context)
        {
            _context = context;
            _options = new JsonSerializerOptions()
            {
                Converters =
                {
                    new JsonStringEnumMemberConverter(),
                },
            };
        }

        public enum ChannelAdmissionAdditinals
        {
            [Description("channelMemberProduct")]
            ChannelMemberProduct,
        }


        public async Task<ChannelAdmissionResponse> GetChannelAdmissionAsync(string channelId, params ChannelAdmissionAdditinals[] additinals)
        {
            if (channelId.StartsWith("ch"))
            {
                channelId = channelId.Remove(0, 2);
            }

            NameValueCollection dict = new NameValueCollection() 
            {
                { "_frontendId",  "6" },
            };

            foreach (var add in additinals)
            {
                dict.Add("additionalResources", add.GetDescription());
            }

            var url = new StringBuilder("https://public-api.ch.nicovideo.jp/v2/open/channels/")
                .Append(channelId)
                .AppendQueryString(dict)
                .ToString();

            return await _context.GetJsonAsAsync<ChannelAdmissionResponse>(url, _options);
        }


        public Task<ChannelInfo> GetChannelInfoAsync(string channelId)
        {
            string channelIdNumberOnly = channelId;

            if (channelId.StartsWith("ch") && channelId.Skip(2).All(c => c >= '0' && c <= '9'))
            {
                channelIdNumberOnly = channelId.Remove(0, 2);
            }

            if (channelIdNumberOnly.All(c => c >= '0' && c <= '9'))
            {
                return _context.GetJsonAsAsync<ChannelInfo>($"http://ch.nicovideo.jp/api/ch.info/{channelIdNumberOnly}");
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    public class ChannelInfo
    {

        [JsonPropertyName("channel_id")]
        public int ChannelId { get; set; }

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
