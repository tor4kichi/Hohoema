#nullable enable
using Hohoema.Models.Niconico.Video;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed class HiddenVideoOwnerRemoveCommand : CommandBase
{
    private readonly VideoFilteringSettings _ngSettings;

    public HiddenVideoOwnerRemoveCommand(VideoFilteringSettings ngSettings)
    {
        _ngSettings = ngSettings;
    }

    protected override bool CanExecute(object parameter)
    {
        if (parameter is IVideoContentProvider provider)
        {
            if (provider.ProviderId != null)
            {
                return _ngSettings.IsHiddenVideoOwnerId(provider.ProviderId);
            }
        }

        return false;
    }

    protected override void Execute(object parameter)
    {
        if (parameter is IVideoContentProvider provider)
        {
            if (provider.ProviderId != null)
            {
                _ngSettings.RemoveHiddenVideoOwnerId(provider.ProviderId);
            }
        }
    }
}
