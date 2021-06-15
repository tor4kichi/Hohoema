using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.NicoRepo
{
    public sealed class NicoRepoEntriesResponse : ResponseWithMeta<NicoRepoMeta>
    {
        [JsonPropertyName("data")]
        public NicoRepoEntry[] Data { get; set; }

        [JsonPropertyName("errors")]
        public Error[] Errors { get; set; }
    }


    public sealed class Error
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("errorReasonCodes")]
        public string[] ErrorReasonCodes { get; set; }
    }


    public sealed class NicoRepoEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("updated")]
        public DateTimeOffset Updated { get; set; }

        [JsonPropertyName("watchContext")]
        public WatchContext WatchContext { get; set; }

        [JsonPropertyName("muteContext")]
        public MuteContext MuteContext { get; set; }

        [JsonPropertyName("actor")]
        public Actor Actor { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("object")]
        public Object Object { get; set; }


        public string GetContentId()
        {
            return Object.Url.Segments.Last();
        }

        public NicoRepoMuteContextTrigger GetMuteContextTrigger()
        {
            return MuteContext != null ? NicoRepoItemTopicExtension.ToNicoRepoTopicType(MuteContext.Trigger) : NicoRepoMuteContextTrigger.Unknown;
        }
    }

    public sealed class Actor
    {
        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("icon")]
        public Uri Icon { get; set; }
    }

    public class MuteContext
    {
        [JsonPropertyName("task")]
        public NicoRepoTask Task { get; set; }

        [JsonPropertyName("sender")]
        public Sender Sender { get; set; }

        [JsonPropertyName("trigger")]
        public string Trigger { get; set; }
    }

    public class Sender
    {
        [JsonPropertyName("idType")]
        public SenderIdTypeEnum IdType { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public SenderTypeEnum Type { get; set; }
    }

    public sealed class Object
    {
        [JsonPropertyName("type")]
        public NicoRepoType Type { get; set; }

        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("image")]
        public Uri Image { get; set; }
    }

    public sealed class WatchContext
    {
        [JsonPropertyName("parameter")]
        public Parameter Parameter { get; set; }
    }

    public sealed class Parameter
    {
        [JsonPropertyName("nicorepo")]
        public string Nicorepo { get; set; }
    }

    public sealed class NicoRepoMeta : Meta
    {
        [JsonPropertyName("hasNext")]
        public bool HasNext { get; set; }

        [JsonPropertyName("maxId")]
        public string MaxId { get; set; }

        [JsonPropertyName("minId")]
        public string MinId { get; set; }

        //[JsonPropertyName("errors")]
        //public object[] Errors { get; set; }
    }

    public enum SenderIdTypeEnum { User, Channel };

    public enum NicoRepoTask { Nicorepo };

    public enum SenderTypeEnum { User, Channel };

    public enum NicoRepoType
    {
        [Description("all")]
        All,

        [Description("video")]
        /// <summary>
        /// 動画投稿
        /// </summary>
        Video,

        [Description("program")]
        /// <summary>
        /// 生放送開始
        /// </summary>
        Program,

        [Description("image")]
        /// <summary>
        /// イラスト投稿
        /// </summary>
        Image,

        [Description("comicStory")]
        /// <summary>
        /// マンガ投稿
        /// </summary>
        ComicStory,

        [Description("article")]
        /// <summary>
        /// ブロマガの記事投稿
        /// </summary>
        Article,

        [Description("game")]
        Game,
    }

}
