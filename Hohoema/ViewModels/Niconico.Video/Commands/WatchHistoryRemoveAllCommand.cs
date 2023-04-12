#nullable enable
using Hohoema.Services.Niconico;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed class WatchHistoryRemoveAllCommand : CommandBase
{
    private readonly WatchHistoryManager _watchHistoryManager;

    public WatchHistoryRemoveAllCommand(
        WatchHistoryManager watchHistoryManager
        )
    {
        _watchHistoryManager = watchHistoryManager;
    }

    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override void Execute(object parameter)
    {
        _ = _watchHistoryManager.RemoveAllHistoriesAsync();
    }
}
