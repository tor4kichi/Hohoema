using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using AngleSharp.Dom;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Net;
using System.Collections.Specialized;
using NiconicoToolkit.Video.Watch.Dmc;
using Windows.Web.Http;

namespace NiconicoToolkit.Video.Watch
{
    public sealed class VideoWatchSubClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _options;

        public VideoWatchSubClient(NiconicoContext context, JsonSerializerOptions options)
        {
            _context = context;
            _options = options;
        }


        public async Task<WatchPageResponse> GetInitialWatchDataAsync(string videoId, bool incrementViewCounter = true, bool addHistory = true, CancellationToken ct = default)
        {
            NameValueCollection dict = new();
            if (incrementViewCounter == false)
            {
                dict.Add("increment_view_counter", "false");
            }

            if (addHistory == false)
            {
                dict.Add("add_history", "false");
            }

            var url = new StringBuilder("https://www.nicovideo.jp/watch/")
                .Append(videoId)
                .AppendQueryString(dict)
                .ToString();

            await _context.WaitPageAccess();
            var res = await _context.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                throw new WebException("Video watch loading failed. status code : " + res.StatusCode);
            }

            var parser = new HtmlParser();
            using (var stream = await res.Content.ReadAsInputStreamAsync())
            using (var document = await parser.ParseDocumentAsync(stream.AsStreamForRead(), ct))
            {
                // ログイン済みの場合
                var canWatchPageNode = document.QuerySelector("#js-initial-watch-data");
                if (canWatchPageNode == null)
                {
                    // ログインしてない場合
                    var requireActionPageNode = document.QuerySelector("body > div.content.WatchAppContainer");
                    if (requireActionPageNode == null)
                    {
                        throw new NiconicoToolkitException("動画視聴ページの解析に失敗：不明なページ");
                    }

                    var videoDataText = requireActionPageNode.GetAttribute("data-video");
                    var tagText = requireActionPageNode.GetAttribute("data-tags");

                    return new WatchPageResponse(new RequireActionForGuestWatchPageResponse(
                        JsonSerializer.Deserialize<VideoDataForGuest>(WebUtility.HtmlDecode(videoDataText), _options),
                        JsonSerializer.Deserialize<TagsForGuest>(WebUtility.HtmlDecode(tagText), _options)
                        ));
                }
                else
                {
                    var watchDataString = canWatchPageNode.GetAttribute("data-api-data");
                    var environmentString = canWatchPageNode.GetAttribute("data-environment");
                    return new WatchPageResponse(new DmcWatchApiResponse(
                        JsonSerializer.Deserialize<DmcWatchApiData>(WebUtility.HtmlDecode(watchDataString), _options),
                        JsonSerializer.Deserialize<DmcWatchApiEnvironment>(WebUtility.HtmlDecode(environmentString), _options)
                        ));
                }
            }
        }




        public async Task<DmcWatchApiData> GetDmcWatchJsonAsync(string videoId, bool isLoggedIn, string actionTrackId)
        {
            var dict = new NameValueCollection();
            dict.Add("_frontendId", "6");
            dict.Add("_frontendVersion", "0");
            dict.Add("actionTrackId", actionTrackId);
            dict.Add("skips", "harmful");
            dict.Add("additionals", WebUtility.UrlEncode("pcWatchPage,external,marquee,series"));
            dict.Add("isContinueWatching", "true");
            dict.Add("i18nLanguage", "ja-jp");
            dict.Add("t", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString());

            var url = new StringBuilder("https://www.nicovideo.jp/api/watch/")
                .Append(isLoggedIn ? "v3" : "v3_guest")
                .Append("/")
                .Append(videoId)
                .AppendQueryString(dict)
                .ToString();

            try
            {
                var res = await _context.SendAsync(HttpMethod.Get, url, null, headers => 
                {
                    headers.Add("Accept", "*/*");
                    headers.Referer = new Uri($"https://www.nicovideo.jp/watch/{videoId}");
                    headers.Add("Sec-Fetch-Site", "same-origin");
                    headers.Host = new Windows.Networking.HostName("www.nicovideo.jp");
                });

                if (res.ReasonPhrase == "Forbidden")
                {
                    throw new WebException("require payment.");
                }

                return await res.Content.ReadAsAsync<DmcWatchApiData>(_options);
            }
            catch (Exception e)
            {
                throw new WebException("access failed watch/" + videoId, e);
            }
        }

        public async Task<DmcSessionResponse> GetDmcSessionResponseAsync(
            DmcWatchApiData watchData,
            VideoContent videoQuality,
            AudioContent audioQuality,
            bool hlsMode = false
            )
        {
            var req = CreateDmcSessioRequest(watchData, videoQuality, audioQuality, hlsMode);
            var sessionUrl = $"{watchData.Media.Delivery.Movie.Session.Urls[0].UrlUnsafe}?_format=json";
            var requestJson = JsonSerializer.Serialize(req, new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
#if WINDOWS_UWP
            return await _context.PostJsonAsAsync<DmcSessionResponse>(
                sessionUrl,
                new HttpStringContent(requestJson, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json")
                );
#else
            return await _context.PostAsync(sessionUrl, new StringContent(decodedJson, UnicodeEncoding.UTF8, "application/json"));
#endif
        }

        private DmcSessionRequest CreateDmcSessioRequest(
                DmcWatchApiData watchData,
                VideoContent videoQuality = null,
                AudioContent audioQuality = null,
                bool hlsMode = false
                )
        {
            var req = new DmcSessionRequest();

            var session = watchData.Media.Delivery.Movie.Session;
            var videoQualities = watchData.Media.Delivery.Movie.Videos;
            var audioQualities = watchData.Media.Delivery.Movie.Audios;
            var encryption = watchData.Media.Delivery.Encryption;

            // リクエストする動画品質を決定します
            // モバイルの時は最後の動画品質をモバイル画質として断定して指定
            // それ以外の場合、対象画質とそれ以下の有効な画質をすべて指定
            var requestVideoQuality = new List<string>();
            if (videoQuality?.IsAvailable ?? false)
            {
                requestVideoQuality.Add(videoQuality.Id);
            }
            else
            {
                requestVideoQuality.AddRange(videoQualities.Where(x => x.IsAvailable).Select(x => x.Id));
            }

            var requestAudioQuality = new List<string>();
            if (audioQuality?.IsAvailable ?? false)
            {
                requestAudioQuality.Add(audioQuality.Id);
            }
            else
            {
                requestAudioQuality.AddRange(audioQualities.Where(x => x.IsAvailable).Select(x => x.Id));
            }

            var sessionUrl = $"{session.Urls[0].UrlUnsafe}?_format=json";
            var useSsl = true; // session.Urls[0].IsSsl;
            var wellKnownPort = session.Urls[0].IsWellKnownPort;
            var protocolName = session.Protocols[0]; // http,hls
            var protocolAuthType = session.Protocols.ElementAtOrDefault(1) ?? "ht2"; // ht2
            req.Session = new RequestSession()
            {
                RecipeId = session.RecipeId,
                ContentId = session.ContentId,
                ContentType = "movie",
                ContentSrcIdSets = new List<ContentSrcIdSet>()
                {
                    new ContentSrcIdSet()
                    {
                        ContentSrcIds = new List<ContentSrcId>()
                        {
                            new ContentSrcId()
                            {
                                SrcIdToMux = new SrcIdToMux()
                                {
                                    VideoSrcIds = requestVideoQuality,
                                    AudioSrcIds = requestAudioQuality
                                },
                            }
                        }
                    }
                },
                TimingConstraint = "unlimited",
                KeepMethod = new KeepMethod()
                {
                    Heartbeat = new Heartbeat() { Lifetime = 120000 }
                },
                Protocol = new Protocol()
                {
                    Name = "http",
                    Parameters = new Protocol.ProtocolParameters()
                    {
                        HttpParameters = new Protocol.HttpParameters()
                        {
                            Parameters = new Protocol.ParametersInfo()
                            {
                                HlsParameters = hlsMode 
                                    ? new Protocol.HlsParameters()
                                    {
                                        UseSsl = useSsl ? "yes" : "no",
                                        UseWellKnownPort = wellKnownPort ? "yes" : "no",
                                        SegmentDuration = 6000,
                                        TransferPreset = "",
                                        Encryption = 
                                            new Protocol.Encryption() 
                                            {
                                                HlsEncryptionV1 = encryption != null ? new Protocol.HlsEncryptionV1() 
                                                {
                                                    EncryptedKey = encryption.EncryptedKey,
                                                    KeyUri = encryption.KeyUri
                                                }
                                                : null
                                                , Empty = encryption == null ? new Protocol.Empty(): null
                                            }
                                            
                                    } 
                                    : null,
                                HttpOutputDownloadParameters = !hlsMode ? new Protocol.HttpOutputDownloadParameters() : null
                            }
                        }
                    }
                },
                ContentUri = "",
                SessionOperationAuth = new SessionOperationAuth_Request()
                {
                    SessionOperationAuthBySignature = new SessionOperationAuthBySignature_Request()
                    {
                        Token = session.Token,
                        Signature = session.Signature
                    }
                },
                ContentAuth = new ContentAuth_Request()
                {
                    AuthType = "ht2",
                    ContentKeyTimeout = (int)session.ContentKeyTimeout,
                    ServiceId = "nicovideo",
                    ServiceUserId = session.ServiceUserId
                },
                ClientInfo = new ClientInfo()
                {
                    PlayerId = session.PlayerId
                },
                Priority = session.Priority
            };

            return req;

            /*
            var requestJson = JsonSerializer.Serialize(req);
            var decodedJson = requestJson;
    #if WINDOWS_UWP
            return await context.PostAsync(sessionUrl, new HttpStringContent(decodedJson, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
    #else
                return await context.PostAsync(sessionUrl, new StringContent(decodedJson, UnicodeEncoding.UTF8, "application/json"));
    #endif
            */
        }



        public async Task DmcSessionFirstHeartbeatAsync(
            DmcWatchApiData watch,
            DmcSessionResponse sessionRes
            )
        {
            var session = watch.Media.Delivery.Movie.Session;
            var sessionUrl = $"{session.Urls[0].UrlUnsafe}/{sessionRes.Data.Session.Id}?_format=json&_method=PUT";

            var result = await _context.SendAsync(HttpMethod.Options, sessionUrl, content: null,
                (headers) =>
                {
                    headers.Add("Access-Control-Request-Method", "POST");
                    headers.Add("Access-Control-Request-Headers", "content-type");
#if WINDOWS_UWP
                    headers.UserAgent.Add(_context.HttpClient.DefaultRequestHeaders.UserAgent.First());
#else
                    headers.UserAgent.Add(_context.HttpClient.DefaultRequestHeaders.UserAgent.First());
#endif               
                }
                , HttpCompletionOption.ResponseHeadersRead
                );
            if (!result.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine(result.ToString());
            }
        }


        public async Task DmcSessionHeartbeatAsync(
           DmcWatchApiData watch,
           DmcSessionResponse sessionRes
           )
        {
            var session = watch.Media.Delivery.Movie.Session;
            var sessionUrl = $"{session.Urls[0].UrlUnsafe}/{sessionRes.Data.Session.Id}?_format=json&_method=PUT";
            //var message = new HttpRequestMessage(HttpMethod.Post, new Uri(sessionUrl));
            var requestJson = JsonSerializer.Serialize(sessionRes.Data, new JsonSerializerOptions()  { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull } );
#if WINDOWS_UWP
            var content = new HttpStringContent(requestJson, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
#else
            var content = new StringContent(requestJson, UnicodeEncoding.UTF8, "application/json");
#endif

            var result = await _context.SendAsync(HttpMethod.Post, sessionUrl, content, 
                (headers) => 
                {
                    headers.UserAgent.Add(_context.HttpClient.DefaultRequestHeaders.UserAgent.First());
                    headers.Add("Origin", "http://www.nicovideo.jp");
                    headers.Add("Referer", "http://www.nicovideo.jp/watch/" + watch.Video.Id);
                    headers.Add("Accept", "application/json");
                }
                , HttpCompletionOption.ResponseHeadersRead
                );

            if (!result.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine(result.ToString());
            }
        }

        public async Task DmcSessionExitHeartbeatAsync(
            DmcWatchApiData watch,
            DmcSessionResponse sessionRes
            )
        {
            var session = watch.Media.Delivery.Movie.Session;
            var sessionUrl = $"{session.Urls[0].UrlUnsafe}/{sessionRes.Data.Session.Id}?_format=json&_method=DELETE";

            var requestJson = JsonSerializer.Serialize(sessionRes.Data, new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

#if WINDOWS_UWP
            var content = new HttpStringContent(requestJson, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
#else
            var content = new StringContent(requestJson, UnicodeEncoding.UTF8, "application/json");
#endif
            var result = await _context.SendAsync(HttpMethod.Post, sessionUrl, content,
               (headers) =>
               {
                   headers.Add("Access-Control-Request-Method", "POST");
                   headers.Add("Access-Control-Request-Headers", "content-type");
                   headers.UserAgent.Add(_context.HttpClient.DefaultRequestHeaders.UserAgent.First());
                   headers.Add("Accept", "application/json");
               }
               , HttpCompletionOption.ResponseHeadersRead
               );

            if (!result.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine(result.ToString());
            }
        }


        public async Task DmcSessionLeaveAsync(
            DmcWatchApiData watch,
            DmcSessionResponse sessionRes
            )
        {
            var session = watch.Media.Delivery.Movie.Session;
            var sessionUrl = $"{session.Urls[0].UrlUnsafe}/{sessionRes.Data.Session.Id}?_format=json&_method=DELETE";

            var result = await _context.SendAsync(HttpMethod.Options, sessionUrl, content: null, headers => 
            {
                headers.Add("Access-Control-Request-Method", "POST");
                headers.Add("Access-Control-Request-Headers", "content-type");
                headers.UserAgent.Add(_context.HttpClient.DefaultRequestHeaders.UserAgent.FirstOrDefault());
            }
            , HttpCompletionOption.ResponseHeadersRead
            );
            if (!result.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine(result.ToString());
            }
        }

        #region nvapi Watch

        public async Task<bool> SendOfficialHlsWatchAsync(
            string contentId,
            string trackId
            )
        {
            var uri = new Uri($"https://nvapi.nicovideo.jp/v1/2ab0cbaa/watch?t={Uri.EscapeDataString(trackId)}");
            var refererUri = new Uri($"https://www.nicovideo.jp/watch/{contentId}");

            {
                var optionRes = await _context.SendAsync(HttpMethod.Options, uri, null, 
                    headers => 
                    {
                        headers.Add("Access-Control-Request-Headers", "x-frontend-id,x-frontend-version");
                        headers.Add("Access-Control-Request-Method", "GET");
                        headers.Add("Origin", "https://www.nicovideo.jp");
#if WINDOWS_UWP
                        headers.Referer = refererUri;
#else
                        headers.Referrer = refererUri;
#endif
                    }
                    , HttpCompletionOption.ResponseHeadersRead
                    );

                if (!optionRes.IsSuccessStatusCode) { return false; }
            }
            {
                var watchRes = await _context.SendAsync(HttpMethod.Get, uri, null, 
                    headers => 
                    {
                        headers.Add("X-Frontend-Id", "6");
                        headers.Add("X-Frontend-Version", "0");
#if WINDOWS_UWP
                        headers.Referer = refererUri;
#else
                        headers.Referrer = refererUri;
#endif
                        headers.Add("Origin", "https://www.nicovideo.jp");

                    }
                    , HttpCompletionOption.ResponseHeadersRead
                    );

                return watchRes.IsSuccessStatusCode;
            }
        }

        #endregion

    }
}
