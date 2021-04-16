using System;
using System.Reflection;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;

namespace Microsoft.Toolkit.Uwp.Helpers
{
    internal class JsonObjectSerializer : IObjectSerializer
    {
        public T Deserialize<T>(object value)
        {
            var type = typeof(T);
            var typeInfo = type.GetTypeInfo();

            // Note: If you're creating a new app, you could just use the serializer directly.
            // This if/return combo is to maintain compatibility with 6.1.1
            if (typeInfo.IsPrimitive || type == typeof(string))
            {
                return (T)Convert.ChangeType(value, type);
            }

            return JsonConvert.DeserializeObject<T>((string)value);
        }

        public object Serialize<T>(T value)
        {
            var type = typeof(T);
            var typeInfo = type.GetTypeInfo();

            // Note: If you're creating a new app, you could just use the serializer directly.
            // This if/return combo is to maintain compatibility with 6.1.1
            if (typeInfo.IsPrimitive || type == typeof(string))
            {
                return value;
            }

            return JsonConvert.SerializeObject(value);
        }
    }
}
