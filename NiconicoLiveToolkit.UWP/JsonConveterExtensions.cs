using System.Text.Json;

namespace NiconicoLiveToolkit
{
    internal static class JsonConveterExtensions
    {
        public static T ToObject<T>(this JsonElement element, JsonSerializerOptions options = null)
        {
            var json = element.GetRawText();
            return JsonSerializer.Deserialize<T>(json, options);
        }
        public static T ToObject<T>(this JsonDocument document, JsonSerializerOptions options = null)
        {
            var json = document.RootElement.GetRawText();
            return JsonSerializer.Deserialize<T>(json, options);
        }
    }

}
