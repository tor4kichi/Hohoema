using Hohoema.Models.Domain.Niconico.Video;
using LiteDB;
using System;

namespace Hohoema.Models.Domain.VideoCache
{
    public sealed class VideoCacheEntity
    {
        [BsonId]
        public string VideoId { get; set; }

        public string FileName { get; set; }

        public string Title { get; internal set; }

        public NicoVideoQuality RequestedVideoQuality { get; set; }

        public NicoVideoQuality DownloadedVideoQuality { get; set; }

        public VideoCacheStatus Status { get; set; }
        
        public VideoCacheDownloadOperationFailedReason FailedReason { get; set; }

        public long? TotalBytes { get; set; }

        public long? ProgressBytes { get; set; }

        public DateTime RequestedAt { get; set; }

        public int SortIndex { get; set; }
    }
}
