using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Users.User;
using Mntone.Nico2.Users.Video;
using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            await WaitNicoPageAccess();

            var userRes = await Context.User.GetUserAsync(userId);

            var owner = NicoVideoOwnerDb.Get(userId);
            if (userRes.Status == "ok")
            {
                var user = userRes.User;
                if (owner == null)
                {
                    owner = new NicoVideoOwner()
                    {
                        OwnerId = userId,
                        UserType = UserType.User
                    };
                }
                owner.ScreenName = user.Nickname;
                owner.IconUrl = user.ThumbnailUrl;

                NicoVideoOwnerDb.AddOrUpdate(owner);
            }

            return owner;
        }


        public async Task<UserDetail> GetUserDetail(string userId)
        {
            await WaitNicoPageAccess();

            var detail = await Context.User.GetUserDetail(userId);

            var owner = NicoVideoOwnerDb.Get(userId);
            if (detail != null)
            {
                if (owner == null)
                {
                    owner = new NicoVideoOwner()
                    {
                        OwnerId = userId,
                        UserType = UserType.User
                    };
                }
                owner.ScreenName = detail.Nickname;
                owner.IconUrl = detail.ThumbnailUri;


                NicoVideoOwnerDb.AddOrUpdate(owner);
            }

            return detail;
        }


        public async Task<UserVideoResponse> GetUserVideos(uint userId, uint page, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
        {
            await WaitNicoPageAccess();

            return await Context.User.GetUserVideos(userId, page, sort, order);
        }



        public async Task<List<MylistGroupData>> GetUserMylistGroups(string userId)
        {
            await WaitNicoPageAccess();

            return await Context.Mylist.GetUserMylistGroupAsync(userId);
        }
    }
}
