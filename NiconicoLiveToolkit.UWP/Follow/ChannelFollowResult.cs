using NiconicoToolkit.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Follow
{
    public class ChannelFollowResult
    {

        [JsonPropertyName("channel_id")]
        public ChannelId ChannelId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        private bool? _IsSucceed;


        /// <summary>
        /// フォローの追加や削除に成功した場合に true を示します。<br />
        /// 既に追加済み、解除済みだった場合も true を示します。
        /// </summary>
        public bool IsSucceed
        {
            get { return (_IsSucceed ?? (_IsSucceed = Status == "succeed")).Value; }
        }

    }
}
