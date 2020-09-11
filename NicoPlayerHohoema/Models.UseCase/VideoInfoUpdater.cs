using Mntone.Nico2;
using Mntone.Nico2.Searches.Video;
using MonkeyCache;
using Newtonsoft.Json;
using Hohoema.Database;
using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Video;

using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using NiconicoSession = Hohoema.Models.Domain.NiconicoSession;
using Hohoema.Models.Domain.Application;

namespace Hohoema.Models.UseCase
{
    public sealed class VideoInfoUpdater 
    {
        private readonly NiconicoSession _niconicoSession;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly IScheduler _scheduler;
        private readonly IBarrel _barrel;

        public VideoInfoUpdater(
            Models.Domain.NiconicoSession niconicoSession,
            NicoVideoProvider nicoVideoProvider,
            IScheduler scheduler,
            IBarrel barrel
            )
        {
            _niconicoSession = niconicoSession;
            _nicoVideoProvider = nicoVideoProvider;
            _scheduler = scheduler;
            _barrel = barrel;
        }

        public async Task UpdateAsync(IVideoContentWritable video)
        {
            if (_niconicoSession.ServiceStatus.IsOutOfService())
            {
                return;
            }

            if (string.IsNullOrEmpty(video?.Id)) 
            {
                return; 
            }

            NicoVideo info = await _nicoVideoProvider.GetNicoVideoInfo(video.Id);

            if (info == null) { return; }

            _scheduler.Schedule(() =>
            {
                video.Label = info.Title;
                video.ViewCount = (int)info.ViewCount;
                video.MylistCount = (int)info.MylistCount;
                video.CommentCount = (int)info.CommentCount;
                video.Length = info.Length;
                video.Description = info.Description;
                video.IsDeleted = info.IsDeleted;
                video.ThumbnailUrl = info.ThumbnailUrl;
                video.PostedAt = info.PostedAt;
                if (info.Owner != null)
                {
                    video.ProviderId = info.Owner.OwnerId;
                    video.ProviderType = info.Owner.UserType;
                }
            });
        }

        
        
    }
}
