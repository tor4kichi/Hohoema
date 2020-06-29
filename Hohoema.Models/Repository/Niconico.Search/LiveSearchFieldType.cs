using Mntone.Nico2.Searches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Search
{
    public enum LiveSearchFieldType
    {
        None = 0x0000_0000,
        All = 0x4FFF_FFFF,

        ContentId = SearchFieldType.ContentId,
        Title = SearchFieldType.Title,
        Description = SearchFieldType.Description,
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
        ThumbnailUrl = SearchFieldType.ThumbnailUrl,
        CommunityText = SearchFieldType.CommunityText,
        CommunityIcon = SearchFieldType.CommunityIcon,
        MemberOnly = SearchFieldType.MemberOnly,
        LiveStatus = SearchFieldType.LiveStatus,
    }
}
