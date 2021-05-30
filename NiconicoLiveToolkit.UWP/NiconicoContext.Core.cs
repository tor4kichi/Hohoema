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
using NiconicoToolkit.Live.Search;
using Windows.Storage.Streams;
using System.IO;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using NiconicoToolkit.Activity;
using NiconicoToolkit.Search;
using NiconicoToolkit.Recommend;
using NiconicoToolkit.Channels;
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

        public NiconicoContext(
            HttpClient httpClient
            )
        {
            HttpClient = httpClient;
            Live = new LiveClient(this);
            Account = new AccountClient(this);
            User = new UserClient(this);
            Video = new VideoClient(this);
            Activity = new ActivityClient(this);
            Search = new SearchClient(this);
            Recommend = new RecommendClient(this);
            Channel = new ChannelClient(this);
        }

        TimeSpan _minPageAccessInterval = TimeSpan.FromSeconds(1);
        DateTime _prevPageAccessTime;
        internal async Task WaitPageAccess()
        {
            var elapsedTime = DateTime.Now - _prevPageAccessTime;
            if (elapsedTime < _minPageAccessInterval)
            {
                await Task.Delay(_minPageAccessInterval - elapsedTime);
            }

            _prevPageAccessTime = DateTime.Now + TimeSpan.FromSeconds(1);
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

        public HttpClient HttpClient { get; }

        public AccountClient Account { get; }
        public LiveClient Live { get; }
        public UserClient User { get; }
        public VideoClient Video { get; }
        public ActivityClient Activity { get; }
        public SearchClient Search { get; }
        public RecommendClient Recommend { get; }
        public ChannelClient Channel { get; }

        #region 

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
            var res = await HttpClient.GetAsync(path);
            return await res.Content.ReadAsAsync<T>(options);
        }

#if WINDOWS_UWP
        internal Task<HttpResponseMessage> PostAsync(string path, IHttpContent httpContent, CancellationToken ct = default)
        {
            return HttpClient.PostAsync(new Uri(path), httpContent).AsTask(ct);
        }

        internal Task<HttpResponseMessage> PostAsync(Uri path, IHttpContent httpContent, CancellationToken ct = default)
        {
            return HttpClient.PostAsync(path, httpContent).AsTask(ct);
        }


        internal Task<HttpResponseMessage> PostAsync(string path, IEnumerable<KeyValuePair<string, string>> form, CancellationToken ct = default)
        {
            var content = new HttpFormUrlEncodedContent(form);
            return HttpClient.PostAsync(new Uri(path), content).AsTask(ct);
        }

        internal Task<HttpResponseMessage> PostAsync(Uri path, IEnumerable<KeyValuePair<string, string>> form, CancellationToken ct = default)
        {
            var content = new HttpFormUrlEncodedContent(form);
            return HttpClient.PostAsync(path, content).AsTask(ct);
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


        internal Task<T> PostJsonAsAsync<T>(string path, Dictionary<string, string> pairs, JsonSerializerOptions options = null)
        {
            return PostJsonAsAsync<T>(new Uri(path), pairs, options);
        }

        internal async Task<T> PostJsonAsAsync<T>(Uri path, Dictionary<string, string> pairs, JsonSerializerOptions options = null)
        {
            var pairsContent = pairs;
#if WINDOWS_UWP
            var content = new HttpFormUrlEncodedContent(pairsContent);
#else
            var content = new FormUrlEncodedContent(pairsContent);
#endif
            return await PostJsonAsAsync<T>(path, content);
        }

#if WINDOWS_UWP

        internal Task<T> PostJsonAsAsync<T>(string path, IHttpContent content, JsonSerializerOptions options = null)
        {
            return PostJsonAsAsync<T>(new Uri(path), content, options);
        }

        internal async Task<T> PostJsonAsAsync<T>(Uri path, IHttpContent content, JsonSerializerOptions options = null)
        {
            var res = await PostAsync(path, content);
            return await res.Content.ReadAsAsync<T>(options);
        }
#endif

#if WINDOWS_UWP

        internal Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, string path, IHttpContent content = null, Action<HttpRequestHeaderCollection> headerFiller = null, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken ct = default)
        {
            return SendAsync(httpMethod, new Uri(path), content, headerFiller, completionOption, ct);
        }

        internal async Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, Uri path, IHttpContent content = null, Action<HttpRequestHeaderCollection> headerFiller = null, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken ct = default)
        {
            var req = new HttpRequestMessage(httpMethod, path);
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
            var req = new HttpRequestMessage(httpMethod, path);
            req.Content = content;
            headerFiller?.Invoke(req.Headers);
            return await HttpClient.SendAsync(req, completionOption, ct);
        }
#endif


        internal Task<T> DeleteJsonAsAsync<T>(string path, JsonSerializerOptions options = null, CancellationToken ct = default)
        {
            return DeleteJsonAsAsync<T>(new Uri(path), options, ct);
        }

        internal async Task<T> DeleteJsonAsAsync<T>(Uri path, JsonSerializerOptions options = null, CancellationToken ct = default)
        {
            var res = await SendAsync(HttpMethod.Delete, path, ct: ct);
            return await res.Content.ReadAsAsync<T>(options, ct);
        }

        #endregion
    }

    internal static class HttpContentExtensions
    {
#if WINDOWS_UWP
        public static async Task<T> ReadAsAsync<T>(this IHttpContent httpContent, JsonSerializerOptions options = null, CancellationToken ct = default)
        {
            var inputStream = await httpContent.ReadAsInputStreamAsync();
            using (var stream = inputStream.AsStreamForRead())
            {
                return await JsonSerializer.DeserializeAsync<T>(stream, options, ct);
            }
        }
#else
        public static async Task<T> ReadAsAsync<T>(this HttpContent httpContent, JsonSerializerOptions options = null, CancellationToken ct = default)
        {
            using (var stream = await httpContent.ReadAsStreamAsync())
            {
                return await JsonSerializer.DeserializeAsync<T>(stream, options, ct);
            }
        }
#endif
    }

}
