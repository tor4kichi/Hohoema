#nullable enable
using Hohoema.Infra;
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System.Threading.Tasks;
using UserDetailResponse = NiconicoToolkit.User.UserDetailResponse;

namespace Hohoema.Models.Niconico.User;

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

    public async Task<string> GetUserNameAsync(UserId userId)
    {
        try
        {
            UserNickname userName = await _niconicoSession.ToolkitContext.User.GetUserNicknameAsync(userId);

            if (userName != null)
            {
                NicoVideoOwner owner = _nicoVideoOwnerRepository.Get(userId);
                owner ??= new NicoVideoOwner()
                {
                    OwnerId = userId,
                    UserType = OwnerType.User
                };
                owner.ScreenName = userName.Nickname;
                _ = _nicoVideoOwnerRepository.UpdateItem(owner);
            }

            return userName.Nickname;
        }
        catch
        {
            throw;
        }
    }

    public async Task<NicoVideoOwner> GetUserInfoAsync(UserId userId)
    {
        var (_, owner) = await GetUserDetailAsync(userId);
        return owner;
    }


    public async Task<(UserDetailResponse Response, NicoVideoOwner Owner)> GetUserDetailAsync(UserId userId)
    {
        UserDetailResponse detail = await _niconicoSession.ToolkitContext.User.GetUserDetailAsync(userId);

        NicoVideoOwner owner = _nicoVideoOwnerRepository.Get(userId);
        if (detail.IsSuccess)
        {
            owner ??= new NicoVideoOwner()
            {
                OwnerId = userId,
                UserType = OwnerType.User
            };
            Data ownerData = detail.Data;
            owner.ScreenName = ownerData.User.Nickname;
            owner.IconUrl = ownerData.User.Icons.Small.OriginalString;


            _ = _nicoVideoOwnerRepository.UpdateItem(owner);
        }

        return (detail, owner);
    }


    public async Task<NiconicoToolkit.User.UserVideoResponse> GetUserVideosAsync(UserId userId, int page = 0, int pageSize = 100, UserVideoSortKey sortKey = UserVideoSortKey.RegisteredAt, UserVideoSortOrder sortOrder = UserVideoSortOrder.Desc)
    {
        UserVideoResponse res = await _niconicoSession.ToolkitContext.User.GetUserVideoAsync(userId, page, pageSize, sortKey, sortOrder);

        if (res.IsSuccess)
        {
            foreach (UserVideoItem item in res.Data.Items)
            {
                _ = _nicoVideoProvider.UpdateCache(item.Essential.Id, item.Essential);
            }
        }

        return res;
    }
}
