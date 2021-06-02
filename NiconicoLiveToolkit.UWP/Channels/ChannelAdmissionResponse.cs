using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Channels
{
    public partial class ChannelAdmissionResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public ChannelAdmissionData Data { get; set; }
    }

    public partial class ChannelAdmissionData
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("status")]
        public ChannelStatus Status { get; set; }

        [JsonPropertyName("configuration")]
        public Configuration Configuration { get; set; }

        [JsonPropertyName("location")]
        public Location Location { get; set; }

        [JsonPropertyName("additional")]
        public Additional Additional { get; set; }
    }

    public partial class Additional
    {
        [JsonPropertyName("channelMemberProduct")]
        public ChannelMemberProduct ChannelMemberProduct { get; set; }
    }

    public partial class ChannelMemberProduct
    {
        [JsonPropertyName("channelId")]
        public long ChannelId { get; set; }

        [JsonPropertyName("price")]
        public Price Price { get; set; }
    }

    public partial class Price
    {
        [JsonPropertyName("taxIncludedPrice")]
        public long TaxIncludedPrice { get; set; }

        [JsonPropertyName("taxExcludedPrice")]
        public long TaxExcludedPrice { get; set; }
    }

    public partial class Configuration
    {
        [JsonPropertyName("basic")]
        public Basic Basic { get; set; }

        [JsonPropertyName("exposure")]
        public Exposure Exposure { get; set; }

        [JsonPropertyName("admission")]
        public Admission Admission { get; set; }
    }

    public partial class Admission
    {
        [JsonPropertyName("isFree")]
        public bool IsFree { get; set; }

        [JsonPropertyName("isFirstMonthFree")]
        public bool IsFirstMonthFree { get; set; }
    }

    public partial class Basic
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("screenName")]
        public string ScreenName { get; set; }

        [JsonPropertyName("category")]
        public Category Category { get; set; }

        [JsonPropertyName("ownerName")]
        public string OwnerName { get; set; }
    }

    public partial class Category
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public partial class Exposure
    {
        [JsonPropertyName("openTime")]
        public DateTimeOffset? OpenTime { get; set; }

        [JsonPropertyName("closeTime")]
        public DateTimeOffset? CloseTime { get; set; }
    }

    public partial class Location
    {
        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonPropertyName("thumbnailUrl")]
        public Uri ThumbnailUrl { get; set; }

        [JsonPropertyName("thumbnailSmallUrl")]
        public Uri ThumbnailSmallUrl { get; set; }

        [JsonPropertyName("categoryTopPageUrl")]
        public Uri CategoryTopPageUrl { get; set; }
    }

    public partial class ChannelStatus
    {
        [JsonPropertyName("isOpen")]
        public bool IsOpen { get; set; }

        [JsonPropertyName("isAdmissionAvailable")]
        public bool IsAdmissionAvailable { get; set; }

        [JsonPropertyName("isAdultChannel")]
        public bool IsAdultChannel { get; set; }

        [JsonPropertyName("isGravureChannel")]
        public bool IsGravureChannel { get; set; }
    }
}
