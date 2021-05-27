using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NiconicoToolkit
{
    internal static class JsonDeserializeHelper
    {
        public static T Deserialize<T>(string json, JsonSerializerOptions options = null)
        {
            return JsonSerializer.Deserialize<T>(json, options);
        }

        public static T Deserialize<T>(byte[] utf8Bytes, JsonSerializerOptions options = null)
        {
            return JsonSerializer.Deserialize<T>(utf8Bytes, options);
        }

        public static ValueTask<T> DeserializeAsync<T>(Stream utf8Stream, JsonSerializerOptions options = null, CancellationToken ct = default)
        {
            return JsonSerializer.DeserializeAsync<T>(utf8Stream, options, ct);
        }

#if WINDOWS_UWP
        public static T Deserialize<T>(ReadOnlySpan<byte> utf8Bytes, JsonSerializerOptions options = null)
        {
            return JsonSerializer.Deserialize<T>(utf8Bytes, options);
        }
#endif

    }
}
