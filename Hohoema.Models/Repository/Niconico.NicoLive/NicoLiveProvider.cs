using Mntone.Nico2.Nicocas.Live;
using Hohoema.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Niconico;
using Mntone.Nico2.Live.Watch.Crescendo;
using Hohoema.Models.Repository.Niconico.NicoLive;
using Hohoema.Models.Live;

namespace Hohoema.Models.Repository.NicoLive
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
        

        public async Task<Database.NicoLive> GetLiveProgramInfoAsync(string liveId)
        {
            var programInfo = await ContextActionAsync(async context =>
            {
                return await context.Live.GetProgramInfoAsync(liveId);
            });

            if (programInfo?.IsOK ?? false)
            {
                var liveData = NicoLiveDb.Get(liveId) ?? new Database.NicoLive() { LiveId = liveId };

                liveData.Description = programInfo.Data.Description;
                liveData.StartTime = programInfo.Data.BeginAt;
                liveData.EndTime = programInfo.Data.EndAt;
                liveData.OpenTime = programInfo.Data.VposBaseAt;
                liveData.IsMemberOnly = programInfo.Data.IsMemberOnly;
                liveData.CategoryTags = programInfo.Data.Categories.ToList();

                liveData.ProviderType = programInfo.Data.SocialGroup.Type.ToModelCommunityType();
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
                return liveData;
            }
            else
            {
                return null;
            }
        }

        public async Task<LivePlayerProps> GetPlayerPropsAsync(string liveId)
        {
            var res = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.Live.GetCrescendoLeoPlayerPropsAsync(liveId);
            });

            return new LivePlayerProps(res);
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

    public sealed class LivePlayerProps
    {
        private readonly CrescendoLeoProps _props;

        internal LivePlayerProps(Mntone.Nico2.Live.Watch.Crescendo.CrescendoLeoProps props)
        {
            _props = props;
            OpenTime = DateTimeOffset.FromUnixTimeSeconds(_props.Program.OpenTime);
            StartTime = DateTimeOffset.FromUnixTimeSeconds(_props.Program.BeginTime);
            EndTime = DateTimeOffset.FromUnixTimeSeconds(_props.Program.EndTime);

            BroadcasterCommunityType = props.SocialGroup.Type switch
            {
                "channel" => CommunityType.Channel,
                "official" => CommunityType.Official,
                "community" => CommunityType.Community,
                var x => throw new NotSupportedException(x)
            };

            LiveStatus = props.Program.Status switch
            {
                "ON_AIR" => LiveStatusType.OnAir,
                "RELEASED" => LiveStatusType.ComingSoon,
                "ENDED" => LiveStatusType.Closed,
                var x => throw new NotSupportedException(x)
            };
        }

        public LiveStatusType LiveStatus { get; }

        public string BroadcasterId => _props.Program.BroadcastId ?? _props.SocialGroup.Id;

        public string BroadcasterName => _props.SocialGroup.Name;
        public string Title => _props.Program.Title;

        public string Description => _props.Program.Description;

        public DateTimeOffset OpenTime { get; }
        public DateTimeOffset StartTime { get; }
        public DateTimeOffset EndTime { get; }

        public CommunityType BroadcasterCommunityType { get; }
        public string BroadcasterCommunityId => _props.SocialGroup.Id;
        public string SocialGroupId => _props.SocialGroup.Id;


        public string PlayerAudienceToken => _props.Player.AudienceToken;

        public string ReliveWebSocketUrl => _props.Site.Relive.WebSocketUrl;


    }



}
