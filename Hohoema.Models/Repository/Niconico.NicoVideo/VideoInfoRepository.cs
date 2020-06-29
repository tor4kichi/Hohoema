using MonkeyCache;
using Newtonsoft.Json;
using Hohoema.Database;
using Hohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Niconico;

namespace Hohoema.Models.Repository.Niconico.NicoVideo
{
    public sealed class VideoInfoRepository : ProviderBase
    {
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly IScheduler _scheduler;
        private readonly IBarrel _barrel;

        public VideoInfoRepository(
            NiconicoSession niconicoSession,
            NicoVideoProvider nicoVideoProvider,
            IScheduler scheduler,
            IBarrel barrel
            )
            : base(niconicoSession)
        {
            _nicoVideoProvider = nicoVideoProvider;
            _scheduler = scheduler;
            _barrel = barrel;
        }

        JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        public async Task UpdateAsync(IVideoContentWritable video)
        {
            if (NiconicoSession.ServiceStatus.IsOutOfService())
            {
                return;
            }

            if (string.IsNullOrEmpty(video?.Id)) 
            {
                return; 
            }

            Database.NicoVideo info = await _nicoVideoProvider.GetNicoVideoInfo(video.Id);

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

        private async Task<Database.NicoVideo> GetLatestInfo(string videoId)
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.Search.GetVideoInfoAsync(videoId);
            });

            if (res.Status == "ok")
            {
                var video = res.Video;

                var info = new Database.NicoVideo()
                {
                    RawVideoId = videoId,
                    Title = video.Title,
                    VideoId = video.Id,
                    Length = video.Length,
                    PostedAt = video.UploadTime,
                    IsDeleted = video.IsDeleted,
                    Description = video.Description,
                    ViewCount = (int)video.ViewCount,
                    CommentCount = (int)res.Thread.GetCommentCount(),
                    MylistCount = (int)video.MylistCount,
                    ThreadId = video.DefaultThread,
                    ThumbnailUrl = video.ThumbnailUrl.OriginalString,
                    Tags = res.Tags.TagInfo.Select(x => new NicoVideoTag()
                    {
                        Id = x.Tag,
                    }
                    ).ToList()
                };

                if (res.Video.ProviderType == "channel")
                {
                    info.Owner = new NicoVideoOwner()
                    {
                        OwnerId = res.Video.CommunityId,
                        UserType = Database.NicoVideoUserType.Channel
                    };
                }
                else
                {
                    info.Owner = new NicoVideoOwner()
                    {
                        OwnerId = res.Video.UserId,
                        UserType = res.Video.ProviderType == "regular" ? NicoVideoUserType.User : NicoVideoUserType.Channel
                    };
                }

                info.IsDeleted = res.Video.IsDeleted;
                if (info.IsDeleted && int.TryParse(res.Video.__deleted, out int deleteType))
                {
                    try
                    {
                        info.PrivateReasonType = (PrivateReasonType)deleteType;
                    }
                    catch { }
                }

                return info;
            }

            throw new Exception();
        }

        
    }
}
