using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Live.WatchSession.ToClientMessage
{
    internal sealed class CommentSessionToClientMessageJsonConverter : JsonConverter<CommentSessionToClientMessage>
    {
        //[return: MaybeNull]
        public override CommentSessionToClientMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDocument = JsonDocument.ParseValue(ref reader);
            if (jsonDocument.RootElement.TryGetProperty("chat_result", out var chatResultJsonElement))
            {
                return chatResultJsonElement.ToObject<ChatResult_CommentSessionToClientMessage>(options);
            }
            else if (jsonDocument.RootElement.TryGetProperty("chat", out var chatJsonElement))
            {
                return chatJsonElement.ToObject<Chat_CommentSessionToClientMessage>(options);
            }
            else if (jsonDocument.RootElement.TryGetProperty("thread", out var threadJsonElement))
            {
                return threadJsonElement.ToObject<Thread_CommentSessionToClientMessage>(options);
            }
            else if (jsonDocument.RootElement.TryGetProperty("ping", out var pingJsonElement))
            {
                return pingJsonElement.ToObject<Ping_CommentSessionToClientMessage>(options);
            }
            else
            {
                throw new NotSupportedException(jsonDocument.RootElement.GetRawText());
            }
        }

        public override void Write(Utf8JsonWriter writer, CommentSessionToClientMessage value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
