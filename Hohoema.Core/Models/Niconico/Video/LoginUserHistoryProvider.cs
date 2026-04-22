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

    VideoWatchHistory? _lastHistory;
    public async Task<List<VideoWatchHistoryDataItem>> GetHistoryAsync(int page = 0, int pageSize = 100)
    {
        using var releaser = await _niconicoSession.SigninLock.LockAsync();

        if (!_niconicoSession.IsLoggedIn)
        {
            throw new HohoemaException("Failed get login user video watch history. Require LogIn.");
        }

        if (page == 0) { _lastHistory = null; }

        VideoWatchHistory res = await _niconicoSession.ToolkitContext.History.VideoWachHistory.GetWatchHistoryAsync(pageSize, _lastHistory);

        if (res.Meta.IsSuccess is false) { throw new HohoemaException("Failed get login user video watch history"); }

        _lastHistory = res;
        foreach (VideoWatchHistoryDataItem history in res.Data.Items)
        {
            _videoWatchedRepository.VideoPlayedIfNotWatched(history.Video.Id, TimeSpan.MaxValue);
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
