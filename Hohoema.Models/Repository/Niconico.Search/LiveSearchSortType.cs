using Mntone.Nico2.Searches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Search
{
    public enum LiveSearchSortType
    {
        None = 0x0000_0000,

        UserId = SearchFieldType.UserId,
        ChannelId = SearchFieldType.ChannelId,
        CommunityId = SearchFieldType.CommunityId,
        ViewCounter = SearchFieldType.ViewCounter,
        CommentCounter = SearchFieldType.CommentCounter,
        OpenTime = SearchFieldType.OpenTime,
        StartTime = SearchFieldType.StartTime,
        LiveEndTime = SearchFieldType.LiveEndTime,
        ScoreTimeshiftReserved = SearchFieldType.ScoreTimeshiftReserved,

        SortDecsending = 0x0000_0000,
        SortAcsending = 0x4000_0000,
    }
}
