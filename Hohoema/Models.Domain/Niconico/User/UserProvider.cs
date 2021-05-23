using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Users.Mylist;
using Mntone.Nico2.Users.User;
using Mntone.Nico2.Users.Video;
using Hohoema.Database;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mntone.Nico2.Users.User.UserDetailResponse;

namespace Hohoema.Models.Domain.Niconico.User
{
    public sealed class UserProvider : ProviderBase
    {
        private readonly NicoVideoOwnerCacheRepository _nicoVideoOwnerRepository;

        public UserProvider(NiconicoSession niconicoSession,
            NicoVideoOwnerCacheRepository nicoVideoOwnerRepository
            )
            : base(niconicoSession)
        {
            _nicoVideoOwnerRepository = nicoVideoOwnerRepository;
        }

        public async Task<string> GetUserName(string userId)
        {
            try
            {
                var userName = await NiconicoSession.ToolkitContext.User.GetUserNicknameAsync(userId);

                if (userName != null)
                {
                    var owner = _nicoVideoOwnerRepository.Get(userId);
                    if (owner == null)
                    {
                        owner = new NicoVideoOwner()
                        {
                            OwnerId = userId,
                            UserType = NicoVideoUserType.User
                        };
                    }
                    owner.ScreenName = userName.Nickname;
                    _nicoVideoOwnerRepository.UpdateItem(owner);
                }

                return userName.Nickname;
            }
            catch
            {
                throw;
            }
        }

        public async Task<NicoVideoOwner> GetUser(string userId)
        {
            var userRes = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetUserAsync(userId);
            }); 

            var owner = _nicoVideoOwnerRepository.Get(userId);
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

                _nicoVideoOwnerRepository.UpdateItem(owner);
            }

            return owner;
        }


        public async Task<UserDetails> GetUserDetail(string userId)
        {
            var detail = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetUserDetailAsync(userId);
            });

            var owner = _nicoVideoOwnerRepository.Get(userId);
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


                _nicoVideoOwnerRepository.UpdateItem(owner);
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
    }
}
