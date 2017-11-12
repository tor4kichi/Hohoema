using NicoPlayerHohoema.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Db
{
    public static class MigrationToLiteDBHelper
    {
        public static void Migrate()
        {
            MigrateUser();
            MigrateVideo();
        }

        private static void MigrateUser()
        {
            var list = UserInfoDb.GetAll();

            foreach (var user in list)
            {
                var owner = new Database.NicoVideoOwner()
                {
                    OwnerId = user.UserId,
                    IconUrl = user.IconUri,
                    ScreenName = user.Name,
                    UserType = user.UserId.StartsWith("ch") ? Mntone.Nico2.Videos.Thumbnail.UserType.Channel : Mntone.Nico2.Videos.Thumbnail.UserType.User
                };
            }
        }

        private static void MigrateVideo()
        {
            var list = VideoInfoDb.GetAll();

            foreach (var item in list)
            {
                var liteDbEntity = new Database.NicoVideo()
                {
                    RawVideoId = item.RawVideoId,
                    VideoId = item.VideoId,
                    Length = item.Length,
                    PostedAt = item.PostedAt,
                    MylistCount = (int)item.MylistCount,
                    CommentCount = (int)item.CommentCount,
                    ViewCount = (int)item.ViewCount,
                    ThreadId = item.ThreadId,
                    Tags = item.GetTags().Select(x => new NicoVideoTag()
                    {
                        Id = x.Value,
                        Name = x.Value,
                        IsLocked = x.Lock,
                        IsCategory = x.Category
                    }).ToList(),
                    ThumbnailUrl = item.ThumbnailUrl,
                    Title = item.Title,
                    DescriptionWithHtml = item.DescriptionWithHtml,
                    PrivateReasonType = item.PrivateReasonType,
                    Owner = new Database.NicoVideoOwner()
                    {
                        OwnerId = item.UserId.ToString(),
                        UserType = item.UserType,

                        // no has user name in legacy NicoVideoInfo
                        // ScreenName = item.
                    }
                };

                NicoVideoDb.AddOrUpdate(liteDbEntity);
            }

            VideoInfoDb.RemoveRangeAsync(list).ConfigureAwait(false);
        }
    }
}
