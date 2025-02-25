#nullable enable
using Hohoema.Models.Niconico.Video;
using Hohoema.Services.Player.Videos;

namespace Hohoema.ViewModels.Niconico.Live;

public sealed partial class NicoLiveUserIdAddToNGCommand : CommandBase
{
    private readonly CommentFilteringFacade _commentFiltering;
    private readonly NicoVideoOwnerCacheRepository _nicoVideoOwnerRepository;

    public NicoLiveUserIdAddToNGCommand(CommentFilteringFacade playerSettings, NicoVideoOwnerCacheRepository nicoVideoOwnerRepository)
    {
        _commentFiltering = playerSettings;
        _nicoVideoOwnerRepository = nicoVideoOwnerRepository;
    }

    protected override bool CanExecute(object parameter)
    {
        return parameter is string;
    }

    protected override void Execute(object parameter)
    {
        var userId = parameter as string;
        var screenName = _nicoVideoOwnerRepository.Get(userId)?.ScreenName;

        _commentFiltering.AddFilteringCommentOwnerId(userId, screenName);
    }
}
