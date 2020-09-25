using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Buffers;
using NiconicoLiveToolkit.Live;
using NiconicoLiveToolkit.Account;
using System.Text.Json.Serialization;
using NiconicoLiveToolkit.Live.Search;
using Windows.Storage.Streams;
using System.IO;
using NiconicoLiveToolkit.User;
using NiconicoLiveToolkit.Video;
#if WINDOWS_UWP
using Windows.Web.Http;
using Windows.Web.Http.Headers;
#else
using System.Net.Http;
using System.Net.Http.Headers;
#endif

namespace NiconicoLiveToolkit
{
    public sealed partial class NiconicoContext 
    {

        public NiconicoContext(
            HttpClient httpClient
            )
        {
            HttpClient = httpClient;
            HttpClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"{nameof(NiconicoLiveToolkit)}/1.0 (+https://github.com/tor4kichi/NiconicoLiveToolkit)");
            Live = new LiveClient(this);
            Account = new AccountClient(this);
            User = new UserClient(this);
            Video = new VideoClient(this);
        }

        public HttpClient HttpClient { get; }


        public AccountClient Account { get; }

        public LiveClient Live { get; }

        public UserClient User { get; }

        public VideoClient Video { get; }

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
            var res = await HttpClient.PostAsync(path, content);
            return await res.Content.ReadAsAsync<T>(options);
        }

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
