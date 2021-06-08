using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Live.WatchSession.ToClientMessage
{
    internal abstract class CommentSessionToClientMessage
    {
    }


    internal sealed class Chat_CommentSessionToClientMessage : CommentSessionToClientMessage
    {
        //{"chat":{"thread":1672749703,"vpos":30634141,"date":1594472144,"date_usec":40142,
        // "mail":"184","user_id":"FVhOI15-mwu_aJ8YxfaVZ4FzTu0","premium":1,"anonymity":1,
        // "content":"高圧洗浄プラス吸引車で汚泥の除去が鉄板かな…"}}

        [JsonPropertyName("thread")]
        public string Thread { get; set; }

        [JsonPropertyName("vpos")]
        [JsonConverter(typeof(VideoPositionToTimeSpanConverter))]
        public TimeSpan VideoPosition { get; set; }

        [JsonPropertyName("leaf")]
        public int? Leaf { get; set; }

        [JsonPropertyName("no")]
        public int CommentId { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }

        [JsonPropertyName("date_usec")]
        public long DateUsec { get; set; }

        [JsonPropertyName("premium")]
        public int? Premium { get; set; }

        [JsonPropertyName("anonymity")]
        public int? Anonymity { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("mail")]
        public string Mail { get; set; }

        [JsonPropertyName("score")]
        public int? Score { get; set; }

        [JsonPropertyName("deleted")]
        public int? Deleted { get; set; }

        [JsonPropertyName("yourpost")]
        public int? Yourpost { get; set; }

    }

    internal sealed class ChatResult_CommentSessionToClientMessage : CommentSessionToClientMessage
    {
        [JsonPropertyName("thread")]
        public string Thread { get; set; }

        [JsonPropertyName("status")]
        public ChatResult Status { get; set; }

        [JsonPropertyName("no")]
        public int? CommentId { get; set; }
    }

    internal sealed class Thread_CommentSessionToClientMessage : CommentSessionToClientMessage
    {
        [JsonPropertyName("resultcode")]
        public int Resultcode { get; set; }

        [JsonPropertyName("thread")]
        public string Thread { get; set; }

        [JsonPropertyName("server_time")]
        public int ServerTime { get; set; }

        [JsonPropertyName("last_res")]
        public int? LastRes { get; set; }

        [JsonPropertyName("ticket")]
        public string Ticket { get; set; }

        [JsonPropertyName("revision")]
        public int Revision { get; set; }
    }

    internal sealed class Ping_CommentSessionToClientMessage : CommentSessionToClientMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

}
