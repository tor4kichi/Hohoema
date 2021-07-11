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

        public ChannelClient(NiconicoContext context, JsonSerializerOptions defaultOptions)
        {
            _context = context;
            _options = defaultOptions;
        }

        internal static class Urls
        {

        }



        public enum ChannelAdmissionAdditinals
        {
            [Description("channelMemberProduct")]
            ChannelMemberProduct,
        }


        public async Task<ChannelAdmissionResponse> GetChannelAdmissionAsync(ChannelId channelId, params ChannelAdmissionAdditinals[] additinals)
        {
            NameValueCollection dict = new NameValueCollection() 
            {
                { "_frontendId",  "6" },
            };

            foreach (var add in additinals)
            {
                dict.Add("additionalResources", add.GetDescription());
            }

            var url = new StringBuilder(NiconicoUrls.ChannelPublicApiV2Url)
                .Append("open/channels/")
                .Append(channelId.ToStringWithoutPrefix())
                .AppendQueryString(dict)
                .ToString();

            return await _context.GetJsonAsAsync<ChannelAdmissionResponse>(url, _options);
        }


        public Task<ChannelInfo> GetChannelInfoAsync(ChannelId channelId)
        {
            return _context.GetJsonAsAsync<ChannelInfo>($"{NiconicoUrls.ChannelApiUrl}ch.info/{channelId.ToStringWithoutPrefix()}", _options);
        }

        public const int OneTimeItemsCountOnGetChannelVideoAsync = 20;

        public Task<ChannelVideoResponse> GetChannelVideoAsync(ChannelId channelId, int page, ChannelVideoSortKey? sortKey = null, ChannelVideoSortOrder? sortOrder = null)
        {
            return GetChannelVideoAsync_Internal(NiconicoUrls.MakeChannelPageUrl(channelId), page, sortKey, sortOrder);
        }


        public Task<ChannelVideoResponse> GetChannelVideoAsync(string channelIdOrScreenName, int page, ChannelVideoSortKey? sortKey = null, ChannelVideoSortOrder? sortOrder = null)
        {
            return GetChannelVideoAsync_Internal(NiconicoUrls.MakeChannelPageUrl(channelIdOrScreenName), page, sortKey, sortOrder);
        }

        async Task<ChannelVideoResponse> GetChannelVideoAsync_Internal(string channelPageUrl, int page, ChannelVideoSortKey? sortKey, ChannelVideoSortOrder? sortOrder)
        {
            var dict = new NameValueCollection() { { "page", (page + 1).ToString() } };

            if (sortKey is not null) { dict.Add("sort", sortKey.Value.GetDescription()); }
            if (sortOrder is not null) { dict.Add("order", sortOrder.Value.GetDescription()); }

            var url = new StringBuilder(channelPageUrl)
                .Append("/video")
                .AppendQueryString(dict)
                .ToString();

            await _context.WaitPageAccessAsync();

            using var res = await _context.GetAsync(url);

            ChannelVideoResponse channelVideoResponse = new()
            {
                Meta = new Meta() { Status = (int)res.StatusCode }
            };
            
            if (!res.IsSuccessStatusCode) { return channelVideoResponse; }

            return await res.Content.ReadHtmlDocumentActionAsync(document => 
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
                            item.CommentSummary = lastResNode?.TextContent ?? string.Empty;
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
                                    item.MylistCount = countNode.QuerySelector("var").TextContent.ToInt();
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
            });
        }
    }
}
