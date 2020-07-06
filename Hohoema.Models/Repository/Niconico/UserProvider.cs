using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Users.User;
using Mntone.Nico2.Users.Video;
using Hohoema.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Repository.Niconico.NicoVideo;
using Hohoema.Models.Repository.Niconico.NicoVideo.Series;

namespace Hohoema.Models.Repository.Niconico
{
    using NiconicoSession = Models.Niconico.NiconicoSession;

    public sealed class UserProvider : ProviderBase
    {
        public UserProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<NicoVideoOwner> GetUser(string userId)
        {
            var userRes = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetUserAsync(userId);
            }); 

            var owner = NicoVideoOwnerDb.Get(userId);
            if (userRes.Status == "ok")
            {
                var user = userRes.User;
                if (owner == null)
                {
                    owner = new NicoVideoOwner()
                    {
                        OwnerId = userId,
                        UserType = NicoVideoUserType.User
                    };
                }
                owner.ScreenName = user.Nickname;
                owner.IconUrl = user.ThumbnailUrl;

                NicoVideoOwnerDb.AddOrUpdate(owner);
            }

            return owner;
        }


        public async Task<UserDetails> GetUserDetail(string userId)
        {
            var detail = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetUserDetail(userId);
            });

            var owner = NicoVideoOwnerDb.Get(userId);
            if (detail != null)
            {
                if (owner == null)
                {
                    owner = new NicoVideoOwner()
                    {
                        OwnerId = userId,
                        UserType = NicoVideoUserType.User
                    };
                }
                owner.ScreenName = detail.Nickname;
                owner.IconUrl = detail.ThumbnailUri;


                NicoVideoOwnerDb.AddOrUpdate(owner);
            }

            return new UserDetails()
            { 
                UserId = detail.UserId,
                Description = detail.Description,
                BirthDay = detail.BirthDay,
                FollowerCount = detail.FollowerCount,
                Gender = detail.Gender?.ToModelSex(),
                IsOwnerVideoPrivate = detail.IsOwnerVideoPrivate,
                IsPremium = detail.IsPremium,
                Nickname = detail.Nickname,
                Region = detail.Region,
                StampCount = detail.StampCount,
                ThumbnailUri = detail.ThumbnailUri,
                TotalVideoCount = detail.TotalVideoCount,

            };
        }


        public async Task<UserVideosResponse> GetUserVideos(uint userId, uint page, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
        {
            var res = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetUserVideos(userId, page, sort.ToInfrastructureSort(), order.ToInfrastructureOrder());
            });

            return new UserVideosResponse()
            {
                UserId = res.UserId,
                UserName = res.UserName,
                Items = res.Items?.Select(x => 
                {
                    var video = Database.NicoVideoDb.Get(x.VideoId);
                    video.Title = x.Title;
                    video.Length = x.Length;
                    video.Description = x.Description;
                    video.ThumbnailUrl = x.ThumbnailUrl.OriginalString;
                    video.PostedAt = x.SubmitTime;
                    Database.NicoVideoDb.AddOrUpdate(video);

                    return video;
                }).ToList()
            };
        }



        public async Task<List<MylistGroupData>> GetUserMylistGroups(string userId)
        {
            var items = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.Mylist.GetUserMylistGroupAsync(userId);
            });


            return items.Select(x => new MylistGroupData(x)).ToList();
        }




        public async Task<IReadOnlyList<UserSeries>> GetUserSeriesAsync(string userId)
        {
            var res = await ContextActionWithPageAccessWaitAsync(context =>
            {
                return context.User.GetUserSeiresAsync(userId);
            });

            return res?.Serieses.Where(x => x.IsListed).Select(series => new UserSeries(series)).ToList();
        }
    }
}
