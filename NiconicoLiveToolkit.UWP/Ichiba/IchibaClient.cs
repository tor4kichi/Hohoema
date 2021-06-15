using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Ichiba
{
    public sealed class IchibaClient 
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _defaultOptions;

        internal IchibaClient(NiconicoContext context, JsonSerializerOptions defaultOptions)
        {
            _context = context;
            _defaultOptions = defaultOptions;
        }


        internal static class Urls
        {
            public const string IchibaEmbedApiUrl = $"{NiconicoUrls.IchibaPageUrl}embed/";
            public const string IchibaEmbedV3ApiUrl = $"{IchibaEmbedApiUrl}v3/";
        }

        // https://ichiba.nicovideo.jp/embed/v3/show_ichiba?v=sm38853686&country=ja-jp&ch=null&rev=20190628&tags[]=%E3%83%8B%E3%83%A3%E3%83%B3%E3%82%B3%E3%82%B9%E3%82%AD%E3%83%BC&tags[]=%E3%82%B2%E3%83%BC%E3%83%A0%E5%AE%9F%E6%B3%81&tags[]=PS4&tags[]=%E3%83%8B%E3%83%BC%E3%82%A2%E3%83%AC%E3%83%97%E3%83%AA%E3%82%AB%E3%83%B3%E3%83%88&tags[]=%E3%83%8B%E3%83%BC%E3%82%A2&tags[]=NieR&lockedtags[]=%E3%83%8B%E3%83%A3%E3%83%B3%E3%82%B3%E3%82%B9%E3%82%AD%E3%83%BC&lockedtags[]=%E3%82%B2%E3%83%BC%E3%83%A0%E5%AE%9F%E6%B3%81&lockedtags[]=PS4&lockedtags[]=%E3%83%8B%E3%83%BC%E3%82%A2%E3%83%AC%E3%83%97%E3%83%AA%E3%82%AB%E3%83%B3%E3%83%88&lockedtags[]=%E3%83%8B%E3%83%BC%E3%82%A2&lockedtags[]=NieR&video_ids[]=sm38844060


        public async Task<IchibaResponse> GetIchibaItemsAsync(string videoId, string channelId = null, IEnumerable<string> tags = null, IEnumerable<string> lockedTags = null)
        {
            var dict = new NameValueCollection()
            {
                { "v", videoId },
                { "country", "ja-jp" },
                { "rev", "20190628" }
            };

            if (ContentIdHelper.IsChannelId(channelId))
            {
                dict.Add("ch", ContentIdHelper.RemoveContentIdPrefix(channelId));
            }
            else
            {
                dict.Add("ch", "null");
            }

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    dict.Add("tags[]", tag);
                }
            }

            if (lockedTags != null)
            {
                foreach (var tag in lockedTags)
                {
                    dict.Add("lockedtags[]", tag);
                }
            }

            var url = new StringBuilder(Urls.IchibaEmbedV3ApiUrl)
                .Append("show_ichiba")
                .AppendQueryString(dict)
                .ToString();

            try
            {
                var res = await _context.GetJsonAsAsync<IchibaResponse_Internal>(url);

                static IchibaItem ToIchibaItem(AngleSharp.Dom.IElement itemNode)
                {
                    var thumbnailNode = itemNode.QuerySelector("div.IchibaMainItem_Thumbnail > a");
                    var imgNode = thumbnailNode.QuerySelector("img");
                    var itemDetailNode = itemNode.QuerySelector("div.IchibaMainItem_Detail");
                    var itemNameNode = itemDetailNode.QuerySelector("a.IchibaMainItem_Name");
                    var infoNode = itemNode.QuerySelector("div.IchibaMainItem_Info");
                    var shopNode = infoNode?.QuerySelector("span.IchibaMainItem_Info_Shop");
                    var categoryNode = infoNode?.QuerySelector("span.IchibaMainItem_Info_Category");
                    var priceNode = itemNode.QuerySelector("div.IchibaMainItem_Price");
                    var priceNumberNode = priceNode.QuerySelector("span.IchibaMainItem_Price_Number");

                    return new IchibaItem()
                    {
                        AmazonItemLink = thumbnailNode.GetAttribute("href").ToUri(),
                        IchibaUrl = thumbnailNode.GetAttribute("href").ToUri(),
                        Maker = shopNode?.TextContent ?? categoryNode?.TextContent ?? string.Empty,
                        Price = priceNumberNode?.TextContent ?? string.Empty,
                        ThumbnailUrl = imgNode?.GetAttribute("src").ToUri(),
                        Title = WebUtility.HtmlDecode(itemNameNode.TextContent)
                    };
                }

                IchibaResponse ichibaResponse = new IchibaResponse();
                if (res.Main is not null and var mainHtml)
                {
                    HtmlParser parser = new HtmlParser();
                    using (var document = parser.ParseDocument(mainHtml))
                    {
                        ichibaResponse.MainItems = document
                            .QuerySelectorAll("div.IchibaMain_Container.IchibaMain_Container--default > ul > li").Select(ToIchibaItem).ToList();
                    }
                }

                return ichibaResponse;
            }
            catch
            {
                return new IchibaResponse() { MainItems = new List<IchibaItem>(), PickupItems = new List<IchibaItem>() };
            }
        }
    }


    public sealed class Polling
    {
        [JsonPropertyName("shortIntarval")]
        public int ShortIntarval { get; set; }

        [JsonPropertyName("longIntarval")]
        public int LongIntarval { get; set; }

        [JsonPropertyName("defaultIntarval")]
        public int DefaultIntarval { get; set; }

        [JsonPropertyName("maxNoChangeCount")]
        public int MaxNoChangeCount { get; set; }
    }

    internal sealed class IchibaResponse_Internal
    {
        [JsonPropertyName("pickup")]
        public string Pickup { get; set; }

        [JsonPropertyName("main")]
        public string Main { get; set; }

        [JsonPropertyName("polling")]
        public Polling Polling { get; set; }
    }


    public sealed class IchibaResponse
    {
        public List<IchibaItem> MainItems { get; set; }
        public List<IchibaItem> PickupItems { get; set; }
    }



    public class IchibaItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public Uri ThumbnailUrl { get; set; }
        public Uri AmazonItemLink { get; set; }
        public string Maker { get; set; }
        public string Price { get; set; }
        public string DiscountText { get; set; }

        public Uri IchibaUrl { get; set; }

        public IchibaItemReservation Reservation { get; set; }
        public IchibaItemSell Sell { get; set; }
    }


    public class IchibaItemReservation : IchibaItemSellBase
    {
        public string ReleaseDate { get; set; }
        public string ReservationActionText { get; set; }
        public string YesterdayReservationActionText { get; set; }

    }

    public class IchibaItemSell : IchibaItemSellBase
    {
        public string BuyActionText { get; set; }
        public string YesterdayBuyActionText { get; set; }

    }

    public class IchibaItemSellBase
    {
        public string ClickActionText { get; set; }
        public string ClickInThisContentText { get; set; }

    }
}
