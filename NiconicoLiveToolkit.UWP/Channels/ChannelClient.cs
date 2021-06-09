using AngleSharp.Html.Parser;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;

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

        public static string MakeChannelPageUrl(string channelId)
        {
            var directiryName = ChannelIdToURLDirectoryName(channelId);
            return $"https://ch.nicovideo.jp/{directiryName}";
        }

        public static string MakeChannelVideoPageUrl(string channelId)
        {
            var directiryName = ChannelIdToURLDirectoryName(channelId);
            return $"https://ch.nicovideo.jp/{directiryName}/video";
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

            if (channelId.StartsWith("ch") && channelId.Skip(2).All(c => char.IsDigit(c)))
            {
                channelIdNumberOnly = channelId.Remove(0, 2);
            }

            if (!channelIdNumberOnly.All(c => char.IsDigit(c)))
            {
                throw new NotSupportedException();
            }

            return _context.GetJsonAsAsync<ChannelInfo>($"http://ch.nicovideo.jp/api/ch.info/{channelIdNumberOnly}");
        }


        private static string ChannelIdToURLDirectoryName(string channelId)
        {
            if (channelId.StartsWith("ch"))
            {
                if (channelId.Skip(2).All(c => c >= '0' && c <= '9'))
                {
                    return $"channel/{channelId}";
                }
            }
            else if (channelId.All(c => c >= '0' && c <= '9'))
            {
                return $"channel/ch{channelId}";
            }

            return channelId;
        }

        public async Task<ChannelVideoResponse> GetChannelVideoAsync(string channelId, int page, ChannelVideoSortKey? sortKey = null, ChannelVideoSortOrder? sortOrder = null)
        {
            var directoryName = ChannelIdToURLDirectoryName(channelId);
            var dict = new NameValueCollection() { { "page", (page + 1).ToString() } };

            if (sortKey is not null) { dict.Add("sort", sortKey.Value.GetDescription()); }
            if (sortOrder is not null) { dict.Add("order", sortOrder.Value.GetDescription()); }

            var url = new StringBuilder("https://ch.nicovideo.jp/")
                .Append(directoryName)
                .Append("/video")
                .AppendQueryString(dict)
                .ToString();

            await _context.WaitPageAccessAsync();

            var res = await _context.GetAsync(url);

            ChannelVideoResponse channelVideoResponse = new()
            {
                Meta = new Meta() { Status = (int)res.StatusCode }
            };
            
            if (!res.IsSuccessStatusCode) { return channelVideoResponse; }

            HtmlParser parser = new HtmlParser();
            using (var stream = await res.Content.ReadAsInputStreamAsync())
            using (var document = await parser.ParseDocumentAsync(stream.AsStreamForRead()))
            {
                // 件数
                static int GetCount(IHtmlDocument document)
                {
                    var countNode = document.QuerySelector("#video_page > section.site_body > article > section > section > header > span > var");
                    return countNode.TextContent.ToInt();
                }

                static IEnumerable<ChannelVideoItem> GetChannelVideos(IHtmlDocument document)
                {
                    var itemNodes = document.QuerySelectorAll("#video_page > section.site_body > article > section > section > ul > li");
                    foreach (var itemNode in itemNodes)
                    {
                        ChannelVideoItem item = new();
                        {
                            var imageAnchorNode = itemNode.QuerySelector("div.item_left > a");
                            var imageNode = imageAnchorNode.QuerySelector("img");
                            var lastResNode = imageAnchorNode.QuerySelector("span.last_res");
                            var lengthNode = imageAnchorNode.QuerySelector("span.length");

                            item.ThumbnailUrl = imageNode.GetAttribute("src");
                            item.Length = lengthNode.TextContent.ToTimeSpan();
                            item.CommentSummary = lastResNode.TextContent;
                            var ppv = imageAnchorNode.QuerySelector(".purchase_type > span");
                            if (ppv != null)
                            {
                                foreach (var token in ppv.ClassList)
                                {
                                    switch (token)
                                    {
                                        case "all_pay": item.IsRequirePayment = true; break;
                                        case "free_for_member": item.IsFreeForMember = true; break;
                                        case "member_unlimited_access": item.IsMemberUnlimitedAccess = true; break;
                                    }
                                }
                            }
                        }

                        var itemInfoNode = itemNode.QuerySelector("div.item_right");
                        {
                            var titleAnchorNode = itemInfoNode.QuerySelector("h6 > a");
                            var countsNode = itemInfoNode.QuerySelectorAll("ul > li");
                            var descriptionNode = itemInfoNode.QuerySelector("p.description");
                            var timeNode = itemInfoNode.QuerySelector("p.time > time > var");

                            item.ItemId = titleAnchorNode.GetAttribute("href").Split('/').Last();
                            item.Title = titleAnchorNode.GetAttribute("title");
                            foreach (var countNode in countsNode)
                            {
                                if (countNode.ClassList.Contains("view"))
                                {
                                    item.ViewCount = countNode.QuerySelector("var").TextContent.ToInt();
                                }
                                else if (countNode.ClassList.Contains("comment"))
                                {
                                    item.CommentCount = countNode.QuerySelector("var").TextContent.ToInt();
                                }
                                else if (countNode.ClassList.Contains("mylist"))
                                {
                                    item.MylistCount = countNode.QuerySelector("a > var").TextContent.ToInt();
                                }
                            }

                            item.ShortDescription = descriptionNode.TextContent;
                            item.PostedAt = timeNode.GetAttribute("title").ToDateTimeOffsetFromIso8601().DateTime;
                        }

                        yield return item;
                    }
                }

                channelVideoResponse.Data = new()
                {
                    Page = page,
                    TotalCount = GetCount(document),
                    Videos = GetChannelVideos(document).ToArray(),
                };

                return channelVideoResponse;
            }
        }
    }

    public enum ChannelVideoSortKey
    {
        [Description("f")]
        FirstRetrieve,

        [Description("v")]
        ViewCount,

        [Description("r")]
        CommentCount,

        [Description("m")]
        MylistCount,

        [Description("n")]
        NewComment,

        [Description("l")]
        Length,
    }

    public enum ChannelVideoSortOrder
    {
        [Description("d")]
        Desc,
        
        [Description("a")]
        Asc,
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
