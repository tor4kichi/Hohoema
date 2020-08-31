using System;
using System.Collections.Generic;
using System.Text;

namespace NiconicoLiveToolkit.Live.Search
{
    [Flags]
    public enum LiveSearchFilterType
    {
        UserId = SearchFieldType.UserId,
        ChannelId = SearchFieldType.ChannelId,
        CommunityId = SearchFieldType.CommunityId,
        ProviderType = SearchFieldType.ProviderType,
        Tags = SearchFieldType.Tags,
        CategoryTags = SearchFieldType.CategoryTags,
        ViewCounter = SearchFieldType.ViewCounter,
        CommentCounter = SearchFieldType.CommentCounter,
        OpenTime = SearchFieldType.OpenTime,
        StartTime = SearchFieldType.StartTime,
        LiveEndTime = SearchFieldType.LiveEndTime,
        TimeshiftEnabled = SearchFieldType.TimeshiftEnabled,
        ScoreTimeshiftReserved = SearchFieldType.ScoreTimeshiftReserved,
        CommunityText = SearchFieldType.CommunityText,
        MemberOnly = SearchFieldType.MemberOnly,
        LiveStatus = SearchFieldType.LiveStatus,
    }


    [Flags]
    public enum LiveSearchIntegerFilterFieldType
    {
        UserId = SearchFieldType.UserId,
        ChannelId = SearchFieldType.ChannelId,
        CommunityId = SearchFieldType.CommunityId,
        ViewCounter = SearchFieldType.ViewCounter,
        CommentCounter = SearchFieldType.CommentCounter,
        ScoreTimeshiftReserved = SearchFieldType.ScoreTimeshiftReserved,
    }

    [Flags]
    public enum LiveSearchStringFilterFieldType
    {
        Tags = SearchFieldType.Tags,
        CategoryTags = SearchFieldType.CategoryTags,
        CommunityText = SearchFieldType.CommunityText,
    }

    [Flags]
    public enum LiveSearchDateTimeFilterFieldType
    {
        OpenTime = SearchFieldType.OpenTime,
        StartTime = SearchFieldType.StartTime,
        LiveEndTime = SearchFieldType.LiveEndTime,
    }

    [Flags]
    public enum LiveSearchBooleanFilterFieldType
    {
        TimeshiftEnabled = SearchFieldType.TimeshiftEnabled,
        MemberOnly = SearchFieldType.MemberOnly,
    }

}
