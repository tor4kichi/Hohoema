using NiconicoToolkit.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization
{
    internal sealed class MylistIdJsonConverter : JsonConverter<MylistId>
    {
        public override MylistId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt32(),
                JsonTokenType.String => reader.GetString(),
                _ => throw new JsonException(),
            };
        }

        public override void Write(Utf8JsonWriter writer, MylistId value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
