using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Live.WatchSession.ToClientMessage
{
    internal sealed class WatchSessionToClientMessageJsonConverter : JsonConverter<WatchServerToClientMessage>
    {
        //[return: MaybeNullAttribute]
        public override WatchServerToClientMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            var typeProps = document.RootElement.GetProperty("type");
            var typeName = typeProps.GetString();

            if (typeName == "ping")
            {
                return new Ping_WatchSessionToClientMessage();
            }

            JsonElement dataProps;
            if (typeName == "error")
            {
                dataProps = document.RootElement.GetProperty("body");
            }
            else
            {
                dataProps = document.RootElement.GetProperty("data");
            }
            
            return typeName switch
            {
                "error" => dataProps.ToObject<Error_WatchSessionToClientMessage>(options),
                "seat" => dataProps.ToObject<Seat_WatchSessionToClientMessage>(options),
                "akashic" => dataProps.ToObject<Akashic_WatchSessionToClientMessage>(options),
                "postkey" => dataProps.ToObject<Postkey_WatchSessionToClientMessage>(options),
                "stream" => dataProps.ToObject<Stream_WatchSessionToClientMessage>(options),
                "room" => dataProps.ToObject<Room_WatchSessionToClientMessage>(options),
                "rooms" => dataProps.ToObject<Rooms_WatchSessionToClientMessage>(options),
                "serverTime" => dataProps.ToObject<ServerTime_WatchSessionToClientMessage>(options),
                "statistics" => dataProps.ToObject<Statistics_WatchSessionToClientMessage>(options),
                "schedule" => dataProps.ToObject<Schedule_WatchSessionToClientMessage>(options),
                "disconnect" => dataProps.ToObject<Disconnect_WatchSessionToClientMessage>(options),
                "reconnect" => dataProps.ToObject<Reconnect_WatchSessionToClientMessage>(options),
                "postCommentResult" => dataProps.ToObject<PostCommentResult_WatchSessionToClientMessage>(options),

                _ => throw new NotImplementedException(typeName),
            };
        }

        public override void Write(Utf8JsonWriter writer, WatchServerToClientMessage value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

}
