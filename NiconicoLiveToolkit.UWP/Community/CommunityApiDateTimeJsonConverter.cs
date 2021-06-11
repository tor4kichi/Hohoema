using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Community
{
    public sealed class CommunityApiDateTimeJsonConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String) { throw new JsonException(); }

            var dateTimeStr = reader.GetString();
            if (DateTimeOffset.TryParse(dateTimeStr, out var d))
            {
                return d;
            }
            else
            {
                var validDateTimeStr = dateTimeStr.Insert(dateTimeStr.Length - 2, ":");
                if (DateTimeOffset.TryParse(validDateTimeStr, out d))
                {
                    return d;
                }
                else
                {
                    throw new JsonException();
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }
    }
}
