using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Text.Json;

namespace Hohoema.Infra
{
    public class SystemTextJsonSerializer : IObjectSerializer
    {
        static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        {
            Converters =
            {
                new System.Text.Json.Serialization.JsonTimeSpanConverter(),
            }
        };
        
        public T Deserialize<T>(object value)
        {
            return value switch
            {
                string str => System.Text.Json.JsonSerializer.Deserialize<T>(str, _jsonSerializerOptions),
                //bool b when typeof(T) == typeof(bool) => (T)Convert.ChangeType(value, typeof(T)),
                _ => (T)Convert.ChangeType(value, typeof(T)),
            };
        }

        public object Serialize<T>(T value) => System.Text.Json.JsonSerializer.Serialize(value, _jsonSerializerOptions);
    }
}
