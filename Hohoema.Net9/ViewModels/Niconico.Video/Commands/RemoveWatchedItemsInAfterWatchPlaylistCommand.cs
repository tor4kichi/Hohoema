#nullable enable
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using System.Linq;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed partial class RemoveWatchedItemsInAfterWatchPlaylistCommand : CommandBase
{
    private readonly QueuePlaylist _queuePlaylist;
    private readonly VideoWatchedRepository _videoWatchedRepository;

    public RemoveWatchedItemsInAfterWatchPlaylistCommand(
        QueuePlaylist queuePlaylist,
        VideoWatchedRepository videoWatchedRepository
        )
    {
        _queuePlaylist = queuePlaylist;
        _videoWatchedRepository = videoWatchedRepository;
    }

    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override void Execute(object parameter)
    {
        int count = 0;
        foreach (var item in _queuePlaylist.ToArray())
        {
            if (_videoWatchedRepository.IsVideoPlayed(item.VideoId))
            {
                _queuePlaylist.Remove(item);
                count++;
            }
        }

        System.Diagnostics.Debug.WriteLine($"あとで見るから視聴済みを削除 （件数：{count}）");
    }
}
