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
    }
}
