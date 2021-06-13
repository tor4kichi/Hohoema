using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AngleSharp.Dom;
using System.Text.RegularExpressions;
using U8Xml;
#if WINDOWS_UWP
using Windows.Web.Http;
using Windows.Web.Http.Headers;
#else
using System.Net.Http;
using System.Net.Http.Headers;
#endif

namespace NiconicoToolkit.Live.Timeshift
{
    public sealed class TimeshiftClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _defaultOptions;

        internal TimeshiftClient(NiconicoContext context, JsonSerializerOptions defaultOptions)
        {
            _context = context;
            _defaultOptions = defaultOptions;
        }


        internal static class Urls
        {
            public const string TimeshiftReservationApiUrl = $"{NiconicoUrls.NicoLivePageUrl}api/timeshift.reservations";

            public const string MyTimeshiftListHtmlFragmentApiUrl = $"{NiconicoUrls.NicoLivePageUrl}my_timeshift_list";

            public const string MyTimeshiftPageUrl = $"{NiconicoUrls.NicoLivePageUrl}my"; //.php";

            public const string TimeshiftWachingReservationApiUrl = $"http://live.nicovideo.jp/api/watchingreservation";
        }


        private static readonly byte[] s_vidUtf8 = Encoding.UTF8.GetBytes("vid");
        private static readonly byte[] s_titleUtf8 = Encoding.UTF8.GetBytes("title");
        private static readonly byte[] s_statusUtf8 = Encoding.UTF8.GetBytes("status");
        private static readonly byte[] s_unwatchUtf8 = Encoding.UTF8.GetBytes("unwatch");
        private static readonly byte[] s_expireUtf8 = Encoding.UTF8.GetBytes("expire");

        public async Task<TimeshiftReservationsDetailResponse> GetTimeshiftReservationsDetailAsync()
        {
            var res = await _context.GetAsync($"{Urls.TimeshiftWachingReservationApiUrl}?mode=detaillist");

            if (!res.IsSuccessStatusCode)
            {
                return new TimeshiftReservationsDetailResponse() { Meta = new TimeshiftReservationDetailMeta() { Status = res.StatusCode.ToString() } };
            }

            using (var contentStream = await res.Content.ReadAsInputStreamAsync())
            using (var stream = contentStream.AsStreamForRead())
            using (XmlObject xml = XmlParser.Parse(stream))
            {
                XmlNode root = xml.Root;
                var status = root.Attributes.First(x => x.Name.SequenceEqual(s_statusUtf8)).Value.ToString();
                var result = new TimeshiftReservationsDetailResponse() { Meta = new TimeshiftReservationDetailMeta() { Status = status } };
                if (status != "ok")
                {
                    return result;
                }

                static TimeshiftReservationDetailItem ToItem(XmlNode itemNode)
                {
                    var item = new TimeshiftReservationDetailItem();
                    foreach (var valueNode in itemNode.Children)
                    {
                        var nameAsSpan = valueNode.Name.AsSpan();
                        if (valueNode.Name.SequenceEqual(s_vidUtf8))
                        {
                            item.LiveIdWithoutPrefix = valueNode.InnerText.ToString();
                        }
                        else if (valueNode.Name.SequenceEqual(s_titleUtf8))
                        {
                            item.Title = valueNode.InnerText.ToString();
                        }
                        else if (valueNode.Name.SequenceEqual(s_statusUtf8))
                        {
                            item.Status = valueNode.InnerText.ToString();
                        }
                        else if (valueNode.Name.SequenceEqual(s_unwatchUtf8))
                        {
                            item.IsUnwatched = valueNode.InnerText.ToInt32() == 0;
                        }
                        else if (valueNode.Name.SequenceEqual(s_expireUtf8))
                        {
                            var expire = valueNode.InnerText.ToInt64();
                            item.ExpiredAt = expire != 0 ? expire.ToDateTimeOffsetFromUnixTime() : null;
                        }
                    }

                    return item;
                }

                var listNode = root.FirstChild;
                result.Data = new TimeshiftReservationDetailData() 
                {
                    Items = listNode.Value.Children.Select(ToItem).ToArray(),
                };

                return result;
            }
        }




        public Task<ReserveTimeshiftResponse> ReserveTimeshiftAsync(string liveId, bool overwrite)
        {
            if (!ContentIdHelper.IsLiveId(liveId))
            {
                throw new ArgumentException("liveId must contain \"lv\" prefix.");
            }

            var nonPrefixLiveId = ContentIdHelper.RemoveContentIdPrefix(liveId);
            var dict = new NameValueCollection()
            {
                { "vid", nonPrefixLiveId },
                { "overwrite", overwrite.ToString1Or0() },
            };

            return _context.SendJsonAsAsync<ReserveTimeshiftResponse>(httpMethod: HttpMethod.Post, Urls.TimeshiftReservationApiUrl);
        }

        /// <summary>
        /// タイムシフト予約の削除用トークンを取得します。（要ログインセッション）
        /// </summary>
        /// <returns></returns>
        public async Task<ReservationToken> GetReservationToken()
        {
            try
            {
                var res = await _context.GetAsync(Urls.MyTimeshiftListHtmlFragmentApiUrl);

                HtmlParser htmlParser = new HtmlParser();
                using (var contentStream = await res.Content.ReadAsInputStreamAsync())
                using (var stream = contentStream.AsStreamForRead())
                using (var document = await htmlParser.ParseDocumentAsync(stream))
                {
                    var tokenNode = document.QuerySelector("input#confirm");
                    return new ReservationToken(tokenNode.GetAttribute("value"));
                }
            }
            catch
            {
                return ReservationToken.InavalidToken;
            }
        }


        public Task DeleteTimeshiftReservationAsync(string liveId, ReservationToken reservationDeleteToken)
        {
            return DeleteTimeshiftReservationAsync(new string[] { liveId }, reservationDeleteToken);
        }

        public Task DeleteTimeshiftReservationAsync(IEnumerable<string> liveIds, ReservationToken reservationDeleteToken)
        {
            if (ReservationToken.InavalidToken == reservationDeleteToken)
            {

            }

            var dict = new NameValueCollection()
            {
                { "delete", "timeshift" },
                { "confirm", reservationDeleteToken.Token },
            };

            foreach (var liveId in liveIds)
            {
                var nonPrefixLiveId = ContentIdHelper.RemoveContentIdPrefix(liveId);
                dict.Add("vid[]", nonPrefixLiveId);
            }

            var url = new StringBuilder(Urls.MyTimeshiftPageUrl)
                .AppendQueryString(dict)
                .ToString();

            return _context.PostAsync(url);
        }


        public async Task<TimeshiftReservationsResponse> GetTimeshiftReservationsAsync()
        {
            var res = await _context.GetAsync(Urls.MyTimeshiftListHtmlFragmentApiUrl);
            if (!res.IsSuccessStatusCode)
            {
                return ResponseWithMeta.CreateFromStatusCode<TimeshiftReservationsResponse>(res.StatusCode);
            }

            HtmlParser htmlParser = new HtmlParser();           
            using (var contentStream = await res.Content.ReadAsInputStreamAsync())
            using (var stream = contentStream.AsStreamForRead())
            using (var document = await htmlParser.ParseDocumentAsync(stream))
            {
                var tokenNode = document.QuerySelector("input#confirm");
                var token = new ReservationToken(tokenNode.GetAttribute("value"));

                var itemNodes = document.QuerySelectorAll("#liveItemsWrap > form > div > div.column");

                static TimeshiftReservation HtmlElementToReservationItem(IElement itemNode)
                {
                    var nameNode = itemNode.QuerySelector("div.name > a");
                    var liveId = nameNode.GetAttribute("href").Split('/').Last();
                    var statusNode = itemNode.QuerySelector("div.status > span");

                    return new TimeshiftReservation()
                    {
                        Title = nameNode.TextContent,
                        Id = liveId,
                        Status = statusNode.ClassName switch
                        {
                            "timeshift_watch" => TimeshiftStatus.TimeshiftWatch,
                            "timeshift_reservation" => TimeshiftStatus.TimeshiftReservation,
                            "timeshift_disable" => TimeshiftStatus.TimeshiftDisable,
                            _ => TimeshiftStatus.Unknown,
                        },
                        StatusText = statusNode.TextContent,
                    };
                }

                return new TimeshiftReservationsResponse()
                {
                    Meta = new Meta() { Status = (long)res.StatusCode },
                    Data = new TimeshiftReservationsData()
                    { 
                        ReservationToken = token,
                        Items = itemNodes.Select(HtmlElementToReservationItem).ToList()
                    }
                };
            }
        }



        public async Task<UseTimeshiftViewingAuthorityResponse> UseTimeshiftViewingAuthorityAsync(string vid, ReservationToken token)
        {
            var nonPrefixLiveId = ContentIdHelper.RemoveContentIdPrefix(vid);

            var dict = new Dictionary<string, string>()
            {
                { "accept", "true" },
                { "mode", "use" },
                { "vid", nonPrefixLiveId },
                { "token", token.Token},
            };

            var res = await _context.PostAsync(Urls.TimeshiftWachingReservationApiUrl, dict);
            return ResponseWithMeta.CreateFromStatusCode<UseTimeshiftViewingAuthorityResponse>(res.StatusCode);
        }
    }


}
