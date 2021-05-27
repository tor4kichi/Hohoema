using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace NiconicoToolkit.Live.WatchSession
{
    internal sealed class WatchSessionToServerMessageJsonConverter : JsonConverter<WatchClientToServerMessageDataBase>
    { 
        
        //[return: MaybeNullAttribute]
        public override WatchClientToServerMessageDataBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, WatchClientToServerMessageDataBase value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case StartWatching_ToServerMessageData startWatching:
                    JsonSerializer.Serialize(writer, startWatching, options);
                    break;
                case KeepSeat_ToServerMessageData keepSeat:
                    JsonSerializer.Serialize(writer, keepSeat, options);
                    break;
                case GetAkashic_ToServerMessageData getAkashic:
                    JsonSerializer.Serialize(writer, getAkashic, options);
                    break;
                case GetPostkey_ToServerMessageData getPostkey:
                    JsonSerializer.Serialize(writer, getPostkey, options);
                    break;
                case ChangeStream_ToServerMessageData changeStream:
                    JsonSerializer.Serialize(writer, changeStream, options);
                    break;
                case AnswerEnquete_ToServerMessageData answerEnquete:
                    JsonSerializer.Serialize(writer, answerEnquete, options);
                    break;
                case Pong_ToServerMessageData pong:
                    JsonSerializer.Serialize(writer, pong, options);
                    break;
                case PostComment_ToServerMessageData postComment:
                    JsonSerializer.Serialize(writer, postComment, options);
                    break;
                default:
                    throw new NotSupportedException(value.GetType().FullName);
            }
        }
    }
}
