using System;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.IO;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Net;
#if WINDOWS_UWP
using Windows.Web.Http;
#else
using System.Net.Http;
using System.Net.Http.Headers;
#endif

namespace NiconicoToolkit
{
    internal static class HttpContentExtensions
    {
#if WINDOWS_UWP
        public static async Task<T> ReadAsAsync<T>(this IHttpContent httpContent, JsonSerializerOptions options = null, CancellationToken ct = default)
        {
            using (var inputStream = await httpContent.ReadAsInputStreamAsync())
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

        public static async Task<T> ReadHtmlDocumentActionAsync<T>(this IHttpContent httpContent, Func<IHtmlDocument, T> genereterDelegate, HtmlParser parser = null)
        {
            HtmlParser htmlParser = parser ?? new HtmlParser();
            using (var contentStream = await httpContent.ReadAsInputStreamAsync())
            using (var stream = contentStream.AsStreamForRead())
            using (var document = await htmlParser.ParseDocumentAsync(stream))
            {
                return genereterDelegate(document);
            }
        }
    }

}
