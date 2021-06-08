using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.NicoRepo
{
    public sealed class NicoRepoClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _options;

        internal NicoRepoClient(NiconicoContext context, JsonSerializerOptions options)
        {
            _context = context;
            _options = options;
        }

        const string NicorepoTimelineApiUrl = "https://public.api.nicovideo.jp/v1/timelines/nicorepo/last-1-month/my/pc/entries.json";

        public Task<NicoRepoEntriesResponse> GetLoginUserNicoRepoEntriesAsync(NicoRepoType type, NicoRepoDisplayTarget target, string untilId = null)
        {
            NameValueCollection dict = new();
            if (type != NicoRepoType.All)
            {
                dict.Add("object[type]", type.GetDescription());

                dict.Add("type", type switch
                {
                    NicoRepoType.Video => "upload",
                    NicoRepoType.Program => "onair",
                    NicoRepoType.Image => "add",
                    NicoRepoType.ComicStory => "add",
                    NicoRepoType.Article => "add",
                    NicoRepoType.Game => "add",
                    _ => throw new NotSupportedException()
                });
            }

            if (target != NicoRepoDisplayTarget.All) { dict.Add("list", target.GetDescription()); }

            if (untilId != null) { dict.Add("untilId", untilId); }

            var url = new StringBuilder(NicorepoTimelineApiUrl)
                .AppendQueryString(dict)
                .ToString();

            return _context.GetJsonAsAsync<NicoRepoEntriesResponse>(url, _options);
        }
    }




    public enum NicoRepoDisplayTarget
    {
        [Description("all")]
        All,

        [Description("self")]
        Self,

        [Description("followingUser")]
        User,

        [Description("followingChannel")]
        Channel,

        [Description("followingCommunity")]
        Community,

        [Description("followingMylist")]
        Mylist,
    }

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

    public enum NicoRepoMuteContextTrigger
    {
        Unknown,
        NicoVideo_User_Video_Kiriban_Play,
        NicoVideo_User_Video_Upload,
        NicoVideo_Community_Level_Raise,
        NicoVideo_User_Mylist_Add_Video,
        NicoVideo_User_Community_Video_Add,
        NicoVideo_User_Video_UpdateHighestRankings,
        NicoVideo_User_Video_Advertise,
        NicoVideo_Channel_Blomaga_Upload,
        NicoVideo_Channel_Video_Upload,
        Live_User_Program_OnAirs,
        Live_User_Program_Reserve,
        Live_Channel_Program_Onairs,
        Live_Channel_Program_Reserve,
    }

    public static class NicoRepoItemTopicExtension
    {
        public static NicoRepoMuteContextTrigger ToNicoRepoTopicType(string topic) => topic switch
        {
            "video.nicovideo_user_video_upload" => NicoRepoMuteContextTrigger.NicoVideo_User_Video_Upload,
            "video.nicovideo_channel_video_upload" => NicoRepoMuteContextTrigger.NicoVideo_Channel_Video_Upload,
            "program.live_user_program_onairs" => NicoRepoMuteContextTrigger.Live_User_Program_OnAirs,
            "program.live_channel_program_onairs" => NicoRepoMuteContextTrigger.Live_Channel_Program_Onairs,
            "program.live_user_program_reserve" => NicoRepoMuteContextTrigger.Live_User_Program_Reserve,
            "program.live_channel_program_reserve" => NicoRepoMuteContextTrigger.Live_Channel_Program_Reserve,
            "live.user.program.onairs" => NicoRepoMuteContextTrigger.Live_User_Program_OnAirs,
            "live.user.program.reserve" => NicoRepoMuteContextTrigger.Live_User_Program_Reserve,
            "nicovideo.user.video.kiriban.play" => NicoRepoMuteContextTrigger.NicoVideo_User_Video_Kiriban_Play,
            "nicovideo.user.video.upload" => NicoRepoMuteContextTrigger.NicoVideo_User_Video_Upload,
            "nicovideo.community.level.raise" => NicoRepoMuteContextTrigger.NicoVideo_Community_Level_Raise,
            "nicovideo.user.mylist.add.video" => NicoRepoMuteContextTrigger.NicoVideo_User_Mylist_Add_Video,
            "nicovideo.user.community.video.add" => NicoRepoMuteContextTrigger.NicoVideo_User_Community_Video_Add,
            "nicovideo.user.video.update_highest_rankings" => NicoRepoMuteContextTrigger.NicoVideo_User_Video_UpdateHighestRankings,
            "nicovideo.user.video.advertise" => NicoRepoMuteContextTrigger.NicoVideo_User_Video_Advertise,
            "nicovideo.channel.blomaga.upload" => NicoRepoMuteContextTrigger.NicoVideo_Channel_Blomaga_Upload,
            "nicovideo.channel.video.upload" => NicoRepoMuteContextTrigger.NicoVideo_Channel_Video_Upload,
            "live.channel.program.onairs" => NicoRepoMuteContextTrigger.Live_Channel_Program_Onairs,
            "live.channel.program.reserve" => NicoRepoMuteContextTrigger.Live_Channel_Program_Reserve,
            _ => NicoRepoMuteContextTrigger.Unknown,
        };
        
    }

}
