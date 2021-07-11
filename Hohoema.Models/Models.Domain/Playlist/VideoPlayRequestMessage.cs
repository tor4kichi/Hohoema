using Hohoema.Models.Domain.Niconico.Video;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Video;
using System;

namespace Hohoema.Models.Domain.Playlist
{
    public class VideoPlayRequestMessage : AsyncRequestMessage<VideoPlayRequestMessageData>
    {

        public static VideoPlayRequestMessage PlayPlaylist(PlaylistItemToken token)
        {
            return new VideoPlayRequestMessage()
            {
                Playlist= token.Playlist,
                SortOptions = token.SortOptions,
                PlaylistItem = token.Video,
                Index = token.Index,
            };
        }

        public static VideoPlayRequestMessage PlayPlaylist(PlaylistToken token)
        {
            return new VideoPlayRequestMessage()
            {
                Playlist = token.Playlist,
                SortOptions = token.SortOptions,
            };
        }

        public static VideoPlayRequestMessage PlayPlaylist(string playlistId, PlaylistItemsSourceOrigin playlistOrigin, string playlistSortOption)
        {
            return new VideoPlayRequestMessage()
            {
                PlaylistId = playlistId,
                PlaylistOrigin = playlistOrigin,
                PlaylistSortOptionsAsString = playlistSortOption,
            };
        }

        public static VideoPlayRequestMessage PlayPlaylist(string playlistId, PlaylistItemsSourceOrigin playlistOrigin, string playlistSortOption, VideoId videoId, TimeSpan? initialPosition = null)
        {
            return new VideoPlayRequestMessage()
            {
                PlaylistId = playlistId,
                PlaylistOrigin = playlistOrigin,
                PlaylistSortOptionsAsString = playlistSortOption,
                VideoId = videoId,
                Potision = initialPosition,
            };
        }

        public static VideoPlayRequestMessage PlayPlaylist(IPlaylist playlist)
        {
            return new VideoPlayRequestMessage()
            {
                Playlist = playlist,
            };
        }
        public static VideoPlayRequestMessage PlayPlaylist(IPlaylist playlist, IPlaylistSortOption sortOption)
        {
            return new VideoPlayRequestMessage()
            {
                Playlist = playlist,
                SortOptions = sortOption,
            };
        }

        public static VideoPlayRequestMessage PlayPlaylist(IPlaylist playlist, IVideoContent playlistItem, int? index, TimeSpan? initialPosition = null)
        {
            return new VideoPlayRequestMessage()
            {
                Playlist = playlist,
                PlaylistItem = playlistItem,
                Index = index,
                Potision = initialPosition,
            };
        }

        public static VideoPlayRequestMessage PlayPlaylist(IPlaylist playlist, IPlaylistSortOption sortOption, IVideoContent playlistItem, int? index, TimeSpan? initialPosition = null)
        {
            return new VideoPlayRequestMessage()
            {
                Playlist = playlist,
                SortOptions = sortOption,
                PlaylistItem = playlistItem,
                Index = index,
                Potision = initialPosition,
            };
        }

        public static VideoPlayRequestMessage PlayVideoWithQueue(IVideoContent playlistItem, TimeSpan? initialPosition = null)
        {
            return new VideoPlayRequestMessage()
            {
                PlaylistItem = playlistItem,
                Potision = initialPosition,
                PlayWithQueue = true,
            };
        }
        public static VideoPlayRequestMessage PlayVideoWithQueue(VideoId videoId, TimeSpan? initialPosition = null)
        {
            return new VideoPlayRequestMessage()
            {
                VideoId = videoId,
                Potision = initialPosition,
                PlayWithQueue = true,
            };
        }

        public IVideoContent? PlaylistItem { get; init; }
        public IPlaylist Playlist { get; init; }
        public bool? PlayWithQueue { get; init; }
        public PlaylistItemsSourceOrigin? PlaylistOrigin { get; init; }
        public string? PlaylistId { get; init; }
        public string? PlaylistSortOptionsAsString { get; init; }
        public IPlaylistSortOption SortOptions { get; init; }
        public VideoId? VideoId { get; init; }
        public TimeSpan? Potision { get; init; }
        public int? Index { get; init; }
    }

    public record VideoPlayRequestMessageData
    {
        public bool IsSuccess { get; init; }
    }

}
