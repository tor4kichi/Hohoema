using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Live.WatchSession.ToClientMessage
{
    internal abstract class WatchServerToClientMessage
    {

    }

    internal sealed class Error_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        [JsonPropertyName("code")]
        [JsonConverter(typeof(JsonStringEnumMemberConverter))]
        public ErrorMessageType Code { get; set; }
    }

    internal sealed class Seat_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        [JsonPropertyName("keepIntervalSec")]
        public int KeepIntervalSec { get; set; }
    }

    internal sealed class Akashic_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        [JsonPropertyName("status")]
        public AkashicStatus Status { get; set; }

        [JsonPropertyName("playId")]
        public string PlayId { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("playerId")]
        public string PlayerId { get; set; }

        [JsonPropertyName("contentUrl")]
        public string ContentUrl { get; set; }

        [JsonPropertyName("logServerUrl")]
        public string LogServerUrl { get; set; }
    }

    /// <summary>
    /// ポストキー (コメントの投稿に必要なトークン) を通知するメッセージです。
    /// </summary>
    [Obsolete("※ 部屋統合後は取得できません")]
    internal sealed class Postkey_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("expireAt")]
        public DateTime ExpireAt { get; set; }
    }


    internal sealed class Stream_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("syncUri")]
        public string SyncUri { get; set; }

        [JsonPropertyName("quality")]
        public LiveQualityType Quality{ get; set; }

        [JsonPropertyName("availableQualities")]
        public LiveQualityType[] AvailableQualities { get; set; }

        [JsonPropertyName("protocol")]
        public string Protocol { get; set; }
    }


    internal sealed class Room_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        [JsonPropertyName("messageServer")]
        public MessageServer MessageServer { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("threadId")]
        public string ThreadId { get; set; }

        [JsonPropertyName("isFirst")]
        public bool IsFirst { get; set; }

        [JsonPropertyName("waybackkey")]
        [Obsolete("※ 部屋統合後はキーなしで取得できるようにするため空になります")]
        public string waybackkey { get; set; }

        [JsonPropertyName("yourPostKey")]
        [Obsolete("※ 部屋統合までは不要のため空になります")]
        public string YourPostKey { get; set; }
    }

    public sealed class MessageServer
    {
        [JsonPropertyName("uri")]
        public Uri Uri { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    internal sealed class Rooms_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        [JsonPropertyName("rooms")]
        public Room_WatchSessionToClientMessage[] Rooms { get; set; }
    }

    internal sealed class ServerTime_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        [JsonPropertyName("currentMs")]
        public DateTime CurrentTime { get; set; }
    }


    internal sealed class Statistics_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        [JsonPropertyName("viewers")]
        public int? Viewers { get; set; }

        [JsonPropertyName("comments")]
        public int? comments { get; set; }

        [JsonPropertyName("adPoints")]
        public int? adPoints { get; set; }

        [JsonPropertyName("giftPoints")]
        public int? giftPoints { get; set; }
    }


    internal sealed class Schedule_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        [JsonPropertyName("begin")]
        public DateTime Begin { get; set; }

        [JsonPropertyName("end")]
        public DateTime End { get; set; }
    }

    internal sealed class Ping_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        
    }

    internal sealed class Disconnect_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        [JsonPropertyName("reason")]
        public DisconnectReasonType Reason { get; set; }
    }


    internal sealed class Reconnect_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        [JsonPropertyName("audienceToken")]
        public string AudienceToken { get; set; }

        [JsonPropertyName("waitTimeSec")]
        public int WaitTimeSec { get; set; }
    }

    internal sealed class PostCommentResult_WatchSessionToClientMessage : WatchServerToClientMessage
    {
        [JsonPropertyName("chat")]
        public PostCommentResultChat Chat { get; set; }
    }

    public sealed class PostCommentResultChat
    {
        [JsonPropertyName("mail")]
        public string Mail { get; set; }

        [JsonPropertyName("anonymity")]
        public int Anonymity { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("restricted")]
        public bool Restricted { get; set; }
    }

}
