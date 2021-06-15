using Hohoema.Database;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiconicoToolkit.Video;
using NiconicoToolkit.User;
using UserDetailResponse = NiconicoToolkit.User.UserDetailResponse;

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

        public async Task<string> GetUserNameAsync(string userId)
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

        public async Task<NicoVideoOwner> GetUserInfoAsync(string userId)
        {
            var userRes = await _niconicoSession.ToolkitContext.User.GetUserInfoAsync(userId);

            var owner = _nicoVideoOwnerRepository.Get(userId);
            if (userRes.IsOK)
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
                owner.IconUrl = user.ThumbnailUrl.OriginalString;

                _nicoVideoOwnerRepository.UpdateItem(owner);
            }

            return owner;
        }


        public async Task<UserDetailResponse> GetUserDetailAsync(UserId userId)
        {
            var detail = await _niconicoSession.ToolkitContext.User.GetUserDetailAsync(userId);

            var owner = _nicoVideoOwnerRepository.Get(userId);
            if (detail.IsSuccess)
            {
                if (owner == null)
                {
                    owner = new NicoVideoOwner()
                    {
                        OwnerId = userId,
                        UserType = OwnerType.User
                    };
                }
                var ownerData = detail.Data;
                owner.ScreenName = ownerData.User.Nickname;
                owner.IconUrl = ownerData.User.Icons.Small.OriginalString;


                _nicoVideoOwnerRepository.UpdateItem(owner);
            }

            return detail;
        }


        public async Task<NiconicoToolkit.User.UserVideoResponse> GetUserVideosAsync(UserId userId, int page = 0, int pageSize = 100, UserVideoSortKey sortKey = UserVideoSortKey.RegisteredAt, UserVideoSortOrder sortOrder = UserVideoSortOrder.Desc)
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
