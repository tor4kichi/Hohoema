using NicoPlayerHohoema.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Provider
{
    public sealed class NicoLiveProvider : ProviderBase
    {
        public NicoLiveProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }




        public async Task<Mntone.Nico2.Live.Video.NicoliveVideoInfoResponse> GetLiveInfoAsync(string liveId)
        {
            var response = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.Live.GetLiveVideoInfoAsync(liveId);
            });
            
            if (response.IsOK)
            {
                var liveData = NicoLiveDb.Get(liveId) ?? new NicoLive() { LiveId = liveId };

                liveData.Title = response.VideoInfo.Video.Title;
                liveData.Description = response.VideoInfo.Video.Description;
                liveData.TsReservedCount = response.VideoInfo.Video.TsReservedCount;
                liveData.CommentCount = response.VideoInfo.Video.CommentCount;
                liveData.ViewCount = response.VideoInfo.Video.ViewCount;
                liveData.PictureUrl = response.VideoInfo.Video.PictureUrl;
                liveData.ThumbnailUrl = response.VideoInfo.Video.ThumbnailUrl;

                liveData.ProviderType = response.VideoInfo.Video.ProviderType;

                switch (liveData.ProviderType)
                {
                    case Mntone.Nico2.Live.CommunityType.Official:
                        liveData.BroadcasterId = null;
                        liveData.BroadcasterName = "Official";
                        liveData.BroadcasterIconUrl = null;
                        break;
                    case Mntone.Nico2.Live.CommunityType.Community:
                        liveData.BroadcasterId = response.VideoInfo.Community.GlobalId;
                        liveData.BroadcasterName = response.VideoInfo.Community.Name;
                        liveData.BroadcasterIconUrl = response.VideoInfo.Community.ThumbnailSmall;
                        liveData.IsMemberOnly = response.VideoInfo.Video.IsCommunityOnly;
                        break;
                    case Mntone.Nico2.Live.CommunityType.Channel:
                        liveData.BroadcasterId = response.VideoInfo.Video.RelatedChannelId;
                        liveData.BroadcasterName = null;
                        liveData.BroadcasterIconUrl = null;
                        liveData.IsMemberOnly = response.VideoInfo.Video.IsChannelOnly;
                        break;
                    default:
                        break;
                }

                liveData.TimeshiftEnabled = response.VideoInfo.Video.TimeshiftEnabled;
                liveData.TsViewLimitNum = response.VideoInfo.Video.TsViewLimitNum;
                liveData.TimeshiftLimit = response.VideoInfo.Video.TimeshiftLimit;

                liveData.OpenTime = response.VideoInfo.Video.OpenTime.Value;
                liveData.StartTime = response.VideoInfo.Video.StartTime.Value;
                liveData.EndTime = response.VideoInfo.Video.EndTime.Value;

                liveData.TsIsEndless = response.VideoInfo.Video.TsIsEndless;
                liveData.UseTsarchive = response.VideoInfo.Video.UseTsarchive;
                liveData.TsArchiveStartTime = response.VideoInfo.Video.TsArchiveStartTime;
                liveData.TsArchiveReleasedTime = response.VideoInfo.Video.TsArchiveReleasedTime;
                liveData.TsArchiveEndTime = response.VideoInfo.Video.TsArchiveEndTime;

                liveData.CategoryTags = response.VideoInfo.Livetags.Category?.Tags.ToList() ?? new List<string>();
                liveData.LockedTags = response.VideoInfo.Livetags.Locked?.Tags.ToList() ?? new List<string>();
                liveData.FreeTags = response.VideoInfo.Livetags.Free?.Tags.ToList() ?? new List<string>();


                NicoLiveDb.AddOrUpdate(liveData);
            }

            return response;
        }

        public async Task<Mntone.Nico2.Live.Watch.ProgramInfo> GetLiveProgramInfoAsync(string liveId)
        {
            var programInfo = await ContextActionAsync(async context =>
            {
                return await context.Live.GetProgramInfoAsync(liveId);
            });

            if (programInfo?.IsOK ?? false)
            {
                var liveData = NicoLiveDb.Get(liveId) ?? new NicoLive() { LiveId = liveId };

                liveData.Description = programInfo.Data.Description;
                liveData.StartTime = programInfo.Data.BeginAt;
                liveData.EndTime = programInfo.Data.EndAt;
                liveData.OpenTime = programInfo.Data.VposBaseAt;
                liveData.IsMemberOnly = programInfo.Data.IsMemberOnly;
                liveData.CategoryTags = programInfo.Data.Categories.ToList();

                liveData.ProviderType = programInfo.Data.SocialGroup.Type;
                if (programInfo.Data.Broadcaster != null)
                {
                    liveData.BroadcasterName = programInfo.Data.Broadcaster.Name;
                    liveData.BroadcasterId = programInfo.Data.Broadcaster.Id;
                }
                else if (programInfo.Data.SocialGroup != null)
                {
                    liveData.BroadcasterName = programInfo.Data.SocialGroup.Name;
                    liveData.BroadcasterId = programInfo.Data.SocialGroup.Id;
                }

                NicoLiveDb.AddOrUpdate(liveData);
            }

            return programInfo;
        }


        public async Task<Mntone.Nico2.Live.PlayerStatus.PlayerStatusResponse> GetPlayerStatusAsync(string liveId)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Live.GetPlayerStatusAsync(liveId);
            });
        }

        public async Task<Mntone.Nico2.Live.Watch.Crescendo.CrescendoLeoProps> GetLeoPlayerPropsAsync(string liveId)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Live.GetCrescendoLeoPlayerPropsAsync(liveId);
            });
        }

        public async Task<string> GetWaybackKeyAsync(string threadId)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Live.GetWaybackKeyAsync(threadId);
            });
        }


        public async Task<string> GetPostKeyAsync(uint threadId, uint commentCount)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Live.GetPostKeyAsync(threadId, commentCount / 100);
            });
        }
    }
}
