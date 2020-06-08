using Mntone.Nico2.Nicocas.Live;
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
        
        public async Task<NicoCasLiveProgramResponse> GetLiveInfoAsync(string liveId)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Nicocas.GetLiveProgramAsync(liveId);
            });
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
