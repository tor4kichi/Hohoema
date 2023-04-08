using Hohoema.Models.Niconico.Video;
using Hohoema.Services.Niconico;
using System.Collections;
using System.Linq;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed class WatchHistoryRemoveItemCommand : CommandBase
{
    private readonly WatchHistoryManager _watchHistoryManager;

    public WatchHistoryRemoveItemCommand(
        WatchHistoryManager watchHistoryManager
        )
    {
        _watchHistoryManager = watchHistoryManager;
    }

    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override async void Execute(object parameter)
    {
        if (parameter is IVideoContent watchHistory)
        {
            _ = _watchHistoryManager.RemoveHistoryAsync(watchHistory);
        }
        else if (parameter is IList histories)
        {
            await _watchHistoryManager.RemoveHistoryAsync(histories.Cast<IVideoContent>().ToList());
        }
    }
}
