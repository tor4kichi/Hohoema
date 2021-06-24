using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Video;
using System;

namespace Hohoema.Models.Domain.Playlist
{
    public class VideoPlayRequestMessage : AsyncRequestMessage<VideoPlayRequestMessageData>
    {
        public static VideoPlayRequestMessage PlayPlaylist(PlaylistItem playlistItem, TimeSpan? initialPosition = null)
        {
            return new VideoPlayRequestMessage()
            {
                PlaylistItem = playlistItem,
                Potision = initialPosition,
            };
        }

        public static VideoPlayRequestMessage PlayPlaylist(string playlistId, PlaylistItemsSourceOrigin playlistOrigin, string playlistSortOption)
        {
            return new VideoPlayRequestMessage()
            {
                PlaylistId = playlistId,
                PlaylistOrigin = playlistOrigin,
                PlaylistSortOptions = playlistSortOption,
            };
        }

        public static VideoPlayRequestMessage PlayPlaylist(string playlistId, PlaylistItemsSourceOrigin playlistOrigin, string playlistSortOption, VideoId videoId, TimeSpan? initialPosition = null)
        {
            return new VideoPlayRequestMessage()
            {
                PlaylistId = playlistId,
                PlaylistOrigin = playlistOrigin,
                PlaylistSortOptions = playlistSortOption,
                VideoId = videoId,
                Potision = initialPosition,
            };
        }


        public static VideoPlayRequestMessage PlayVideoWithQueue(VideoId videoId, TimeSpan? initialPosition = null)
        {
            return new VideoPlayRequestMessage()
            {
                PlayWithQueue = true,
                VideoId = videoId,
                Potision = initialPosition,
            };
        }

        public PlaylistItem? PlaylistItem { get; init; }
        public bool? PlayWithQueue { get; init; }
        public PlaylistItemsSourceOrigin? PlaylistOrigin { get; init; }
        public string? PlaylistId { get; init; }
        public string? PlaylistSortOptions { get; init; }
        public VideoId? VideoId { get; init; }
        public TimeSpan? Potision { get; init; }
    }

    public record VideoPlayRequestMessageData
    {
        public bool IsSuccess { get; init; }
    }

}
