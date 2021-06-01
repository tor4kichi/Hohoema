using System.IO;
using System.Text.Json;

namespace NiconicoToolkit
{
    internal static class JsonConveterExtensions
    {
        public static T ToObject<T>(this JsonElement element, JsonSerializerOptions options = null)
        {
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                element.WriteTo(writer);
            }
            buffer.Seek(0, SeekOrigin.Begin);
            return JsonSerializer.Deserialize<T>(buffer.ToArray(), options);
        }
        public static T ToObject<T>(this JsonDocument document, JsonSerializerOptions options = null)
        {
            var json = document.RootElement.GetRawText();
            return JsonSerializer.Deserialize<T>(json, options);
        }
    }

}
