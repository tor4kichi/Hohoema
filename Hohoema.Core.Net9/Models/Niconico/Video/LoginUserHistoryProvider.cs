#nullable enable
using Hohoema.Infra;
using NiconicoToolkit.Activity.VideoWatchHistory;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Video;



public sealed class LoginUserVideoWatchHistoryProvider : ProviderBase
{
    private readonly VideoWatchedRepository _videoWatchedRepository;

    public LoginUserVideoWatchHistoryProvider(NiconicoSession niconicoSession,
        VideoWatchedRepository videoWatchedRepository
        )
        : base(niconicoSession)
    {
        _videoWatchedRepository = videoWatchedRepository;
    }

    public async Task<VideoWatchHistoryItem[]> GetHistoryAsync(int page = 0, int pageSize = 100)
    {
        using var releaser = await _niconicoSession.SigninLock.LockAsync();

        if (!_niconicoSession.IsLoggedIn)
        {
            throw new HohoemaException("Failed get login user video watch history. Require LogIn.");
        }

        VideoWatchHistory res = await _niconicoSession.ToolkitContext.History.VideoWachHistory.GetWatchHistoryAsync(page, pageSize);

        if (res.Meta.IsSuccess is false) { throw new HohoemaException("Failed get login user video watch history"); }

        foreach (VideoWatchHistoryItem history in res.Data.Items)
        {
            _ = _videoWatchedRepository.VideoPlayedIfNotWatched(history.WatchId, TimeSpan.MaxValue);
        }

        return res.Data.Items;
    }


    public async Task<bool> RemoveAllHistoriesAsync()
    {
        VideoWatchHistoryDeleteResult res = await _niconicoSession.ToolkitContext.History.VideoWachHistory.DeleteAllWatchHistoriesAsync();
        return res.IsSuccess;
    }

    public async Task<bool> RemoveHistoryAsync(VideoId videoId)
    {
        VideoWatchHistoryDeleteResult res = await _niconicoSession.ToolkitContext.History.VideoWachHistory.DeleteWatchHistoriesAsync(videoId);
        return res.IsSuccess;
    }

    public async Task<bool> RemoveHistoryAsync(IEnumerable<VideoId> videoIdList)
    {
        VideoWatchHistoryDeleteResult res = await _niconicoSession.ToolkitContext.History.VideoWachHistory.DeleteWatchHistoriesAsync(videoIdList);
        return res.IsSuccess;
    }
}
