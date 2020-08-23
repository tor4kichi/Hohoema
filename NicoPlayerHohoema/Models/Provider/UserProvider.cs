using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Users.Mylist;
using Mntone.Nico2.Users.User;
using Mntone.Nico2.Users.Video;
using NicoPlayerHohoema.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mntone.Nico2.Users.User.UserDetailResponse;

namespace NicoPlayerHohoema.Models.Provider
{
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
                return await context.User.GetUserDetailAsync(userId);
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
                owner.ScreenName = detail.User.Nickname;
                owner.IconUrl = detail.User.Icons.Small.OriginalString;


                NicoVideoOwnerDb.AddOrUpdate(owner);
            }

            return detail;
        }


        public async Task<Mntone.Nico2.Videos.Users.UserVideosResponse> GetUserVideos(uint userId, uint page, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
        {
            return await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.Video.GetUserVideosAsync(userId, page/*, sort, order*/);
            });
        }



        public async Task<MylistGroupsResponse> GetUserMylistGroups(string userId)
        {
            return await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetMylistGroupsAsync(int.Parse(userId));
            });
        }
    }
}
