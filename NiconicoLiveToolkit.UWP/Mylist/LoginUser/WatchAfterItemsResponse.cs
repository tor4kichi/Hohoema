using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Mylist.LoginUser
{


    public class WatchAfterItemsResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public MylistGroupItemsData Data { get; set; }
    }



    public class MylistGroupItemsData
    {
        [JsonPropertyName("watchLater")]
        public WatchAfterMylist Mylist { get; set; }
    }

    public class WatchAfterMylist
    {
        [JsonPropertyName("items")]
        public MylistItem[] Items { get; set; }

        [JsonPropertyName("totalCount")]
        public long TotalCount { get; set; }

        [JsonPropertyName("hasNext")]
        public bool HasNext { get; set; }

        [JsonPropertyName("hasInvisibleItems")]
        public bool HasInvisibleItems { get; set; }
    }
}
