#nullable enable
using Hohoema.Infra;
using LiteDB;
using NiconicoToolkit.Video;
using System;

namespace Hohoema.Models.Niconico.Video;

public class VideoWatchedRepository : LiteDBServiceBase<VideoWatchedHistory>
{
    public VideoWatchedRepository(LiteDatabase liteDatabase) 
        : base(liteDatabase, "VideoPlayHistoryEntry" /* 旧名称を残すことで統合処理を省いてる */)
    {
    }

    public void MarkWatched(VideoId videoId)
    {
        VideoPlayed(videoId, TimeSpan.Zero);
    }

    public VideoWatchedHistory VideoPlayed(VideoId videoId, TimeSpan playedPosition)
    {
        VideoWatchedHistory history = _collection.FindById(videoId.ToString());
        if (history != null)
        {
            history.PlayCount++;
        }
        else
        {
            history = new VideoWatchedHistory
            {
                VideoId = videoId,
                PlayCount = 1,
                LastPlayed = DateTime.Now,
                LastPlayedPosition = playedPosition
            };
        }

        _ = _collection.Upsert(history);

        return history;
    }

    public VideoWatchedHistory VideoPlayedIfNotWatched(VideoId videoId, TimeSpan playedPosition)
    {
        VideoWatchedHistory history = _collection.FindById(videoId.ToString());
        if (history != null)
        {
            return history;
        }
        else
        {
            history = new VideoWatchedHistory
            {
                VideoId = videoId,
                PlayCount = 1,
                LastPlayed = DateTime.Now,
                LastPlayedPosition = playedPosition
            };
        }

        _ = _collection.Upsert(history);

        return history;
    }

    public VideoWatchedHistory Get(VideoId videoId)
    {
        return _collection.FindById(videoId.ToString());
    }

    public bool IsVideoPlayed(VideoId videoId)
    {
        return _collection.FindById(videoId.ToString())?.PlayCount > 0;
    }

    public bool IsVideoPlayed(VideoId videoId, out VideoWatchedHistory history)
    {
        VideoWatchedHistory entry = _collection.FindById(videoId.ToString());
        history = entry;
        return entry?.PlayCount > 0;
    }

    public int ClearAllHistories()
    {
        return _collection.DeleteAll();
    }
}

public class VideoWatchedHistory
{
    [BsonId]
    public string VideoId { get; set; }

    public TimeSpan LastPlayedPosition { get; set; }

    public uint PlayCount { get; set; } = 0;

    public DateTime LastPlayed { get; set; }
}
