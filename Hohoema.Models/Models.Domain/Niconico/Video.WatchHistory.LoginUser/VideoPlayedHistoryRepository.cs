﻿using LiteDB;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiconicoToolkit.Video;

namespace Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser
{
    public class VideoPlayedHistoryRepository : LiteDBServiceBase<VideoPlayHistoryEntry>
    {
        public VideoPlayedHistoryRepository(LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        public VideoPlayHistoryEntry VideoPlayed(VideoId videoId, TimeSpan playedPosition)
        {
            var history = _collection.FindById(videoId.ToString());
            if (history != null)
            {
                history.PlayCount++;
            }
            else
            {
                history = new VideoPlayHistoryEntry
                {
                    VideoId = videoId,
                    PlayCount = 1,
                    LastPlayed = DateTime.Now,
                    LastPlayedPosition = playedPosition
                };
            }

            _collection.Upsert(history);

            return history;
        }

        public VideoPlayHistoryEntry VideoPlayedIfNotWatched(VideoId videoId, TimeSpan playedPosition)
        {
            var history = _collection.FindById(videoId.ToString());
            if (history != null)
            {
                return history;
            }
            else
            {
                history = new VideoPlayHistoryEntry
                {
                    VideoId = videoId,
                    PlayCount = 1,
                    LastPlayed = DateTime.Now,
                    LastPlayedPosition = playedPosition
                };
            }

            _collection.Upsert(history);

            return history;
        }

        public VideoPlayHistoryEntry Get(VideoId videoId)
        {
            return _collection.FindById(videoId.ToString());
        }

        public bool IsVideoPlayed(VideoId videoId)
        {
            return _collection.FindById(videoId.ToString())?.PlayCount > 0;
        }

        public bool IsVideoPlayed(VideoId videoId, out VideoPlayHistoryEntry history)
        {
            var entry = _collection.FindById(videoId.ToString());
            history = entry;
            return entry?.PlayCount > 0;
        }

        public int ClearAllHistories()
        {
            return _collection.DeleteAll();
        }
    }

    public class VideoPlayHistoryEntry
    {
        [BsonId]
        public string VideoId { get; set; }

        public TimeSpan LastPlayedPosition { get; set; }

        public uint PlayCount { get; set; } = 0;

        public DateTime LastPlayed { get; set; }
    }
}
