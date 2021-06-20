using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Buffers;
using NiconicoToolkit.Live;
using NiconicoToolkit.Account;
using System.Text.Json.Serialization;
using Windows.Storage.Streams;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using NiconicoToolkit.Activity;
using NiconicoToolkit.SearchWithPage;
using NiconicoToolkit.Recommend;
using NiconicoToolkit.Channels;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.Follow;
using NiconicoToolkit.SearchWithCeApi;
using NiconicoToolkit.Series;
using NiconicoToolkit.NicoRepo;
using NiconicoToolkit.Likes;
using NiconicoToolkit.Community;
using NiconicoToolkit.Ichiba;
using NiconicoToolkit.Live.Timeshift;
#if WINDOWS_UWP
using Windows.Web.Http;
using Windows.Web.Http.Headers;
#else
using System.Net.Http;
using System.Net.Http.Headers;
#endif

namespace NiconicoToolkit
{
    public sealed partial class NiconicoContext 
    {
        public NiconicoContext(string yourSiteUrl)
            : this(new HttpClient())
        {
            HttpClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"{nameof(NiconicoToolkit)}/1.0 (+{yourSiteUrl})");
        }

        internal static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions()
        {
            Converters =
            {
                new JsonStringEnumMemberConverter(),
                new NiconicoIdJsonConverter(),
                new UserIdJsonConverter(),
                new VideoIdJsonConverter(),
                new LiveIdJsonConverter(),
                new MylistIdJsonConverter(),
                new ChannelIdJsonConverter(),
                new CommunityIdJsonConverter(),
            },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

        internal static readonly JsonSerializerOptions DefaultOptionsSnakeCase = new JsonSerializerOptions()
        {
            Converters =
            {
                new JsonStringEnumMemberConverter(JsonSnakeCaseNamingPolicy.Instance),
                new NiconicoIdJsonConverter(),
                new UserIdJsonConverter(),
                new VideoIdJsonConverter(),
                new LiveIdJsonConverter(),
                new MylistIdJsonConverter(),
                new ChannelIdJsonConverter(),
                new CommunityIdJsonConverter(),
            }
        };


        public NiconicoContext(
            HttpClient httpClient
            )
        {
            HttpClient = httpClient;
            Live = new LiveClient(this, DefaultOptions);
            Account = new AccountClient(this);
            User = new UserClient(this, DefaultOptions);
            Video = new VideoClient(this, DefaultOptions);
            Activity = new ActivityClient(this, DefaultOptions);
            SearchWithPage = new SearchWithPageClient(this);
            SearchWithCeApi = new SearchWithCeApiClient(this, DefaultOptions);
            Recommend = new RecommendClient(this, DefaultOptions);
            Channel = new ChannelClient(this, DefaultOptions);
            Mylist = new MylistClient(this, DefaultOptions);
            Follow = new FollowClient(this, DefaultOptions);
            Series = new SeriesClient(this, DefaultOptions);
            NicoRepo = new NicoRepoClient(this, DefaultOptions);
            Likes = new LikesClient(this, DefaultOptions);
            Community = new CommunityClient(this, DefaultOptions);
            Ichiba = new IchibaClient(this, DefaultOptions);
            Timeshift = new TimeshiftClient(this, DefaultOptions);
        }


        public HttpClient HttpClient { get; }

        public AccountClient Account { get; }
        public LiveClient Live { get; }
        public UserClient User { get; }
        public VideoClient Video { get; }
        public ActivityClient Activity { get; }
        public SearchWithPageClient SearchWithPage { get; }
        public SearchWithCeApiClient SearchWithCeApi { get; }
        public RecommendClient Recommend { get; }
        public ChannelClient Channel { get; }
        public MylistClient Mylist { get; }
        public FollowClient Follow { get; }
        public SeriesClient Series { get; }
        public NicoRepoClient NicoRepo { get; }
        public LikesClient Likes { get; }
        public CommunityClient Community { get; }
        public IchibaClient Ichiba { get; }
        public TimeshiftClient Timeshift { get; }


        TimeSpan _minPageAccessInterval = TimeSpan.FromSeconds(1);
        DateTime _prevPageAccessTime;
        internal async ValueTask WaitPageAccessAsync()
        {
            var now = DateTime.Now;
            var elapsedTime = now - _prevPageAccessTime;
            _prevPageAccessTime = now + _minPageAccessInterval;
            if (elapsedTime < _minPageAccessInterval)
            {
                await Task.Delay(_minPageAccessInterval - elapsedTime);
            }
        }

        public void SetupDefaultRequestHeaders()
        {
            HttpClient.DefaultRequestHeaders.Add("Referer", "https://www.nicovideo.jp/");
            HttpClient.DefaultRequestHeaders.Add("X-Frontend-Id", "6");
            HttpClient.DefaultRequestHeaders.Add("X-Frontend-Version", "0");
            HttpClient.DefaultRequestHeaders.Add("X-Niconico-Language", "ja-jp");

            HttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            HttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            HttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
            HttpClient.DefaultRequestHeaders.Add("X-Request-With", "https://www.nicovideo.jp");

            HttpClient.DefaultRequestHeaders.Add("Origin", "https://www.nicovideo.jp");
        }

        #region 

#if WINDOWS_UWP
        internal async Task PrepareCorsAsscessAsync(HttpMethod httpMethod, string uri)
        {
            using var _ =  await SendAsync(httpMethod, uri, content: null, headers => 
            {
                headers.Add("Access-Control-Request-Headers", "x-frontend-id,x-frontend-version,x-niconico-language,x-request-with");
                headers.Add("Access-Control-Request-Method", httpMethod.Method);
            }, HttpCompletionOption.ResponseHeadersRead);
        }
#endif


#if WINDOWS_UWP
        internal Task<HttpResponseMessage> GetAsync(string path, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken ct = default)
        {
            return HttpClient.GetAsync(new Uri(path), completionOption).AsTask(ct);
        }

        internal Task<HttpResponseMessage> GetAsync(Uri path, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken ct = default)
        {
            return HttpClient.GetAsync(path, completionOption).AsTask(ct);
        }
#else
        internal Task<HttpResponseMessage> GetAsync(string path, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken ct = default)
        {
            return HttpClient.GetAsync(path, completionOption, ct);
        }

        internal Task<HttpResponseMessage> GetAsync(Uri path, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken ct = default)
        {
            return HttpClient.GetAsync(path, completionOption, ct);
        }
#endif

        internal Task<string> GetStringAsync(string path)
        {
            return GetStringAsync(new Uri(path));
        }

        internal Task<string> GetStringAsync(Uri path)
        {
            return HttpClient.GetStringAsync(path).AsTask();
        }

        internal Task<T> GetJsonAsAsync<T>(string path, JsonSerializerOptions options = null)
        {
            return GetJsonAsAsync<T>(new Uri(path), options);
        }

        internal async Task<T> GetJsonAsAsync<T>(Uri path, JsonSerializerOptions options = null)
        {
            using var res = await HttpClient.GetAsync(path);
            return await res.Content.ReadAsAsync<T>(options);
        }


#if WINDOWS_UWP
        internal Task<HttpResponseMessage> PostAsync(string path, CancellationToken ct = default)
        {
            return HttpClient.PostAsync(new Uri(path), null).AsTask(ct);
        }

        internal Task<HttpResponseMessage> PostAsync(string path, IHttpContent httpContent, CancellationToken ct = default)
        {
            return HttpClient.PostAsync(new Uri(path), httpContent).AsTask(ct);
        }

        internal Task<HttpResponseMessage> PostAsync(Uri path, IHttpContent httpContent, CancellationToken ct = default)
        {
            return HttpClient.PostAsync(path, httpContent).AsTask(ct);
        }


        internal async Task<HttpResponseMessage> PostAsync(string path, IEnumerable<KeyValuePair<string, string>> form, CancellationToken ct = default)
        {
            using var content = new HttpFormUrlEncodedContent(form);
            return await HttpClient.PostAsync(new Uri(path), content).AsTask(ct);
        }

        internal async Task<HttpResponseMessage> PostAsync(Uri path, IEnumerable<KeyValuePair<string, string>> form, CancellationToken ct = default)
        {
            using var content = new HttpFormUrlEncodedContent(form);
            return await HttpClient.PostAsync(path, content).AsTask(ct);
        }

#else
        internal Task<HttpResponseMessage> PostAsync(string path, HttpContent httpContent, CancellationToken ct = default)
        {
            return HttpClient.PostAsync(path, httpContent, ct);
        }
        internal Task<HttpResponseMessage> PostAsync(Uri path, HttpContent httpContent, CancellationToken ct = default)
        {
            return HttpClient.PostAsync(path, httpContent, ct);
        }

        internal Task<HttpResponseMessage> PostAsync(string path, IEnumerable<KeyValuePair<string, string>> form, CancellationToken ct = default)
        {
            var content = new FormUrlEncodedContent(form);
            return HttpClient.PostAsync(path, content, ct);
        }
        internal Task<HttpResponseMessage> PostAsync(Uri path, IEnumerable<KeyValuePair<string, string>> form, CancellationToken ct = default)
        {
            var content = new FormUrlEncodedContent(form);
            return HttpClient.PostAsync(path, content, ct);
        }
#endif


#if WINDOWS_UWP

        internal Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, string path, IHttpContent content = null, Action<HttpRequestHeaderCollection> headerFiller = null, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken ct = default)
        {
            return SendAsync(httpMethod, new Uri(path), content, headerFiller, completionOption, ct);
        }

        internal async Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, Uri path, IHttpContent content = null, Action<HttpRequestHeaderCollection> headerFiller = null, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(httpMethod, path);
            headerFiller?.Invoke(req.Headers);
            req.Content = content;
            return await HttpClient.SendRequestAsync(req, completionOption).AsTask(ct);
        }
#else
        internal Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, string path, HttpContent content = null, Action<HttpRequestHeaders> headerFiller = null, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken ct = default)
        {
            return SendAsync(httpMethod, new Uri(path), content, headerFiller, completionOption, ct);
        }

        internal async Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, Uri path, HttpContent content, Action<HttpRequestHeaders> headerFiller = null, HttpCompletionOption completionOption = HttpCompletionOption.ResponseHeadersRead, CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(httpMethod, path);
            req.Content = content;
            headerFiller?.Invoke(req.Headers);
            return await HttpClient.SendAsync(req, completionOption, ct);
        }
#endif

#if WINDOWS_UWP
        internal Task<T> SendJsonAsAsync<T>(HttpMethod httpMethod, string url, JsonSerializerOptions options = null, Action<HttpRequestHeaderCollection> headerModifier = null)
#else
		internal Task<T> SendJsonAsAsync<T>(HttpMethod httpMethod, string url, JsonSerializerOptions options = null, Action<System.Net.Http.Headers.HttpRequestHeaders> headerModifier = null)
#endif
        {
            return SendJsonAsAsync<T>(httpMethod, url, httpContent: null, options, headerModifier);
        }



#if WINDOWS_UWP
        internal async Task<T> SendJsonAsAsync<T>(HttpMethod httpMethod, string url, string stringHttpContent, JsonSerializerOptions options = null, Action<HttpRequestHeaderCollection> headerModifier = null)
#else
		internal async Task<T> SendJsonAsAsync<T>(HttpMethod httpMethod, string url, string stringHttpContent, JsonSerializerOptions options = null, Action<System.Net.Http.Headers.HttpRequestHeaders> headerModifier = null)
#endif
        {
#if WINDOWS_UWP
            using var content = new HttpStringContent(stringHttpContent);
#else
            using var content = new HttpStringContent(stringHttpContent);
#endif
            return await SendJsonAsAsync<T>(httpMethod, url, content, options, headerModifier);
        }



#if WINDOWS_UWP
        internal Task<T> SendJsonAsAsync<T>(HttpMethod httpMethod, string url, Dictionary<string, string> pairs, JsonSerializerOptions options = null, Action<HttpRequestHeaderCollection> headerModifier = null)
#else
		internal Task<T> SendJsonAsAsync<T>(HttpMethod httpMethod, string url, Dictionary<string, string> pairs, JsonSerializerOptions options = null, Action<System.Net.Http.Headers.HttpRequestHeaders> headerModifier = null)
#endif
        {
            return SendJsonAsAsync<T>(httpMethod, url, pairs, options, headerModifier);
        }

#if WINDOWS_UWP
        internal async Task<T> SendJsonAsAsync<T>(HttpMethod httpMethod, Uri url, Dictionary<string, string> pairs, JsonSerializerOptions options = null, Action<HttpRequestHeaderCollection> headerModifier = null)
#else
		internal async Task<T> SendJsonAsAsync<T>(HttpMethod httpMethod, Uri url, Dictionary<string, string> pairs, JsonSerializerOptions options = null, Action<System.Net.Http.Headers.HttpRequestHeaders> headerModifier = null)
#endif
        {
#if WINDOWS_UWP
            using var content = new HttpFormUrlEncodedContent(pairs);
#else
            using var content = new FormUrlEncodedContent(pairs);
#endif
            return await SendJsonAsAsync<T>(httpMethod, url, content, options, headerModifier);
        }


#if WINDOWS_UWP
        internal async Task<T> SendJsonAsAsync<T>(HttpMethod httpMethod, string url, IHttpContent httpContent, JsonSerializerOptions options = null, Action<HttpRequestHeaderCollection> headerModifier = null)
#else
		internal Task<T> SendJsonAsAsync<T>(HttpMethod httpMethod, string url, HttpContent httpContent = null, JsonSerializerOptions options = null, Action<System.Net.Http.Headers.HttpRequestHeaders> headerModifier = null)
#endif
        {
            using var message = await SendAsync(httpMethod, new Uri(url), httpContent, headerModifier);
            return await message.Content.ReadAsAsync<T>(options);
        }

#if WINDOWS_UWP
        internal async Task<T> SendJsonAsAsync<T>(HttpMethod httpMethod, Uri url, IHttpContent httpContent, JsonSerializerOptions options = null, Action<HttpRequestHeaderCollection> headerModifier = null)
#else
		internal async Task<T> SendJsonAsAsync<T>(HttpMethod httpMethod, string url, HttpContent httpContent = null, JsonSerializerOptions options = null, Action<System.Net.Http.Headers.HttpRequestHeaders> headerModifier = null)
#endif
        {
            using var message = await SendAsync(httpMethod, url, httpContent, headerModifier);
            return await message.Content.ReadAsAsync<T>(options);
        }

#endregion
    }

}
