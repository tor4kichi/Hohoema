namespace NiconicoToolkit.NicoRepo
{
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
