using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using NiconicoToolkit.Mylist;
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
using NiconicoToolkit.Video;
using NiconicoToolkit.User;

namespace Hohoema.Models.Domain.Niconico.User
{
    public sealed class UserProvider : ProviderBase
    {
        private readonly NicoVideoOwnerCacheRepository _nicoVideoOwnerRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;

        public UserProvider(NiconicoSession niconicoSession,
            NicoVideoOwnerCacheRepository nicoVideoOwnerRepository,
            NicoVideoProvider nicoVideoProvider
            )
            : base(niconicoSession)
        {
            _nicoVideoOwnerRepository = nicoVideoOwnerRepository;
            _nicoVideoProvider = nicoVideoProvider;
        }

        public async Task<string> GetUserName(string userId)
        {
            try
            {
                var userName = await _niconicoSession.ToolkitContext.User.GetUserNicknameAsync(userId);

                if (userName != null)
                {
                    var owner = _nicoVideoOwnerRepository.Get(userId);
                    if (owner == null)
                    {
                        owner = new NicoVideoOwner()
                        {
                            OwnerId = userId,
                            UserType = OwnerType.User
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
                        UserType = OwnerType.User
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
                        UserType = OwnerType.User
                    };
                }
                owner.ScreenName = detail.User.Nickname;
                owner.IconUrl = detail.User.Icons.Small.OriginalString;


                _nicoVideoOwnerRepository.UpdateItem(owner);
            }

            return detail;
        }


        public async Task<NiconicoToolkit.User.UserVideoResponse> GetUserVideos(uint userId, int page = 0, int pageSize = 100, UserVideoSortKey sortKey = UserVideoSortKey.RegisteredAt, UserVideoSortOrder sortOrder = UserVideoSortOrder.Desc)
        {
            var res = await _niconicoSession.ToolkitContext.User.GetUserVideoAsync(userId, page, pageSize, sortKey, sortOrder);

            if (res.IsSuccess)
            {
                foreach (var item in res.Data.Items)
                {
                    _nicoVideoProvider.UpdateCache(item.Essential.Id, item.Essential);
                }
            }

            return res;
        }
    }
}
