using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch
{

    public class TagsForGuest : List<string>
    {

    }

    public partial class VideoDataForGuest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("deleted")]
        public long Deleted { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("main_category_tag")]
        public string MainCategoryTag { get; set; }

        [JsonPropertyName("length_seconds")]
        public long LengthSeconds { get; set; }

        [JsonPropertyName("first_retrieve")]
        public long FirstRetrieve { get; set; }

        [JsonPropertyName("channel_id")]
        public long ChannelId { get; set; }

        [JsonPropertyName("view_counter")]
        public long ViewCounter { get; set; }

        [JsonPropertyName("mylist_counter")]
        public long MylistCounter { get; set; }

        [JsonPropertyName("length")]
        public string Length { get; set; }


        public DateTime GetFirstRetrieve()
        {
            return DateTime.FromFileTimeUtc(FirstRetrieve);
        }
    }

}
