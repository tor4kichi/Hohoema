using System;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.IO;
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
