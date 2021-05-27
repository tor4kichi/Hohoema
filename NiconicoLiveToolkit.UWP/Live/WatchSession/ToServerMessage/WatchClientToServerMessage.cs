using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Live.WatchSession
{
   

    internal sealed class WatchClientToServerMessagePayload
    {
        public WatchClientToServerMessagePayload(WatchClientToServerMessageDataBase data)
        {
            Type = data.Type;
            Data = data;
        }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("data")]
        public WatchClientToServerMessageDataBase Data { get; set; }
    }


    internal abstract class WatchClientToServerMessageDataBase
    {
        [JsonIgnore]
        public string Type { get; set; }

        public WatchClientToServerMessageDataBase(string type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// 視聴開始時に必要な情報を求めるメッセージです。 <br />
    /// 成功の場合、ストリームやメッセージサーバー情報など複数メッセージが順番で返されます。<br />
    /// 失敗の場合、エラーメッセージが返されます。
    /// </summary>
    internal sealed class StartWatching_ToServerMessageData : WatchClientToServerMessageDataBase
    {
        public StartWatching_ToServerMessageData() : base("startWatching") {}

        /// <summary>
        /// 視聴ストリーム関係
        /// </summary>
        [JsonPropertyName("stream")]
        public StartWatchingStream Stream { get; set; }

        [JsonPropertyName("room")]
        public Room Room { get; set; }

        /// <summary>
        /// 座席再利用するかどうかの真偽値。省略時は false。trueの場合、前回取得したストリームを再利用する
        /// </summary>
        [JsonPropertyName("reconnect")]
        public bool Reconnect { get; set; }
    }

    internal sealed class StartWatchingStream
    {
        /// <summary>
        /// 視聴する画質。
        /// </summary>
        [JsonPropertyName("quality")]
        [JsonInclude()]
        public LiveQualityType? Quality { get; set; }

        /// <summary>
        /// 視聴する画質の制限（主にabr用、省略時に無制限）。
        /// </summary>
        [JsonPropertyName("limit")]
        public LiveQualityLimitType? Limit { get; set; }

        /// <summary>
        /// 視聴の遅延を指定する
        /// </summary>
        [JsonPropertyName("latency")]
        [JsonInclude()]
        public LiveLatencyType? Latency { get; set; }

        /// <summary>
        /// 追っかけ再生用のストリームを取得するかどうか。省略時はfalse。
        /// </summary>
        [JsonPropertyName("chasePlay")]
        public bool? ChasePlay { get; set; }
    }


    internal sealed class Room
    {
        [JsonPropertyName("protocol")]
        public string Protocol { get; set; } = "webSocket";

        [JsonPropertyName("commentable")]
        public bool Commentable { get; set; } = true;
    }

    /// <summary>
    /// 座席を維持するためのハートビートメッセージ 継続に視聴するため、定期に(seat.keepIntervalSecごとに)サーバーに送る必要がある
    /// </summary>
    internal sealed class KeepSeat_ToServerMessageData : WatchClientToServerMessageDataBase
    {
        public KeepSeat_ToServerMessageData() : base("keepSeat") { }
    }


    /// <summary>
    /// 新市場機能、生放送ゲームを起動するための情報を取得するためのメッセージです。
    /// </summary>
    internal sealed class GetAkashic_ToServerMessageData : WatchClientToServerMessageDataBase
    {
        public GetAkashic_ToServerMessageData() : base("getAkashic") { }

        /// <summary>
        /// 追っかけ再生かどうか。デフォルトfalse
        /// </summary>
        [JsonPropertyName("chasePlay")]
        public bool? ChasePlay { get; set; }
    }

    /// <summary>
    /// ポストキー (コメントの投稿に必要なトークン) を取得するためのメッセージです。<br />
    /// ※ 部屋統合後は使えなくなります
    /// </summary>
    internal sealed class GetPostkey_ToServerMessageData : WatchClientToServerMessageDataBase
    {
        public GetPostkey_ToServerMessageData() : base("getPostkey") { }
    }

    /// <summary>
    /// 視聴ストリームの送信をサーバーに求めるメッセージです。 <br />
    /// 有効な視聴セッションが既に存在する場合には作成しなおして返します。
    /// </summary>
    internal sealed class ChangeStream_ToServerMessageData : WatchClientToServerMessageDataBase
    {
        public ChangeStream_ToServerMessageData() : base("changeStream") { }

        /// <summary>
        /// 視聴する画質。
        /// </summary>
        [JsonPropertyName("quality")]
        public LiveQualityType? Quality { get; set; }

        /// <summary>
        /// 視聴する画質の制限（主にabr用、省略時に無制限）。
        /// </summary>
        [JsonPropertyName("limit")]
        public LiveQualityLimitType? Limit { get; set; }

        /// <summary>
        /// 視聴の遅延を指定する
        /// </summary>
        [JsonPropertyName("latency")]
        public LiveLatencyType? Latency { get; set; }

        /// <summary>
        /// 追っかけ再生用のストリームを取得するかどうか。省略時はfalse。
        /// </summary>
        [JsonPropertyName("chasePlay")]
        public bool? ChasePlay { get; set; }
    }


    /// <summary>
    /// アンケートの回答を送信するメッセージです。
    /// </summary>
    internal sealed class AnswerEnquete_ToServerMessageData : WatchClientToServerMessageDataBase
    {
        public AnswerEnquete_ToServerMessageData() : base("answerEnquete") { }

        /// <summary>
        /// 回答番号 (0から8までのインデックス)
        /// </summary>
        [JsonPropertyName("answer")]
        public int Answer { get; set; }
    }

    /// <summary>
    /// websocket接続維持のための応答メッセージです。
    /// </summary>
    internal sealed class Pong_ToServerMessageData : WatchClientToServerMessageDataBase
    {
        public Pong_ToServerMessageData() : base("pong") { }
    }


    internal sealed class PostComment_ToServerMessageData : WatchClientToServerMessageDataBase
    {
        public PostComment_ToServerMessageData() : base("postComment") { }

        /// <summary>
        /// コメントの本文
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }


        /// <summary>
        /// コメントの投稿位置 (0.01秒単位)
        /// </summary>
        [JsonPropertyName("vpos")]
        public int Vpos { get; set; }

        /// <summary>
        /// 匿名(184)で投稿するか
        /// </summary>
        [JsonPropertyName("isAnonymous")]
        public bool IsAnonymous { get; set; }

        /// <summary>
        /// コメントサイズ。省略時コメントのサイズが medium になる
        /// </summary>
        [JsonPropertyName("size")]
        public string Size { get; set; }

        /// <summary>
        /// コメント位置。省略時はコメントの位置が naka になる
        /// </summary>
        [JsonPropertyName("position")]
        public string Position { get; set; }

        /// <summary>
        /// コメント色。<br />
        /// 省略時はコメントの色が white になる
        /// </summary>
        [JsonPropertyName("color")]
        public string Color { get; set; }

        /// <summary>
        /// コメントのフォント。省略時はコメントのフォントが defont になる
        /// </summary>
        [JsonPropertyName("font")]
        public string Font { get; set; }
    }

    public enum LiveCommentSize
    {
        Big,
        Medium,
        Small,
    }

    public enum LiveNormalCommentColor
    {
        White,
        Red,
        Pink,
        Orange,
        Yellow,
        Green,
        Cyan,
        Blue,
        Purple,
        Black,
    }

    public enum LivePremiumCommentColor
    {
        White2,
        Red2,
        Pink2,
        Orange2,
        Yellow2,
        Green2,
        Cyan2,
        Blue2,
        Purple2,
        Black2,
    }

    public enum LiveCommentPosition
    {
        Ue,
        Naka,
        Shita,
    }

    public enum LiveCommentFont
    {
        Defont,
        Mincho,
        Gothic,
    }
}
