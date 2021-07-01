using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.LocalMylist
{

    public sealed class LocalPlaylistItemRemovedEventArgs
    {
        public string PlaylistId { get; internal set; }
        public IReadOnlyCollection<VideoId> RemovedItems { get; internal set; }
    }

    public sealed class LocalPlaylistItemAddedEventArgs
    {
        public string PlaylistId { get; internal set; }
        public IReadOnlyCollection<VideoId> AddedItems { get; internal set; }
    }

    public sealed class LocalPlaylistItemAddedMessage : ValueChangedMessage<LocalPlaylistItemAddedEventArgs>
    {
        public LocalPlaylistItemAddedMessage(LocalPlaylistItemAddedEventArgs value) : base(value)
        {
        }
    }

    public sealed class LocalPlaylistItemRemovedMessage : ValueChangedMessage<LocalPlaylistItemRemovedEventArgs>
    {
        public LocalPlaylistItemRemovedMessage(LocalPlaylistItemRemovedEventArgs value) : base(value)
        {
        }
    }



    public sealed class LocalPlaylist : IUserManagedPlaylist
    {
        private readonly LocalMylistRepository _playlistRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly IMessenger _messenger;

        public LocalPlaylist(string id, string label, LocalMylistRepository playlistRepository, NicoVideoProvider nicoVideoProvider, IMessenger messenger)
        {
            PlaylistId = new PlaylistId() { Id = id, Origin = PlaylistItemsSourceOrigin.Local };
            Name = label;
            _playlistRepository = playlistRepository;
            _nicoVideoProvider = nicoVideoProvider;
            _messenger = messenger;

            Count = _playlistRepository.GetCount(PlaylistId.Id);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public string Name { get; private set; }

        public void UpdateLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) { throw new InvalidOperationException(); }

            if (string.Compare(Name, label) != 0)
            {
                Name = label;
                UpdatePlaylistInfo();
            }
        }

        public int Count { get; private set; }

        public int SortIndex { get; set; }

        Uri[] _thumbnailImages = new Uri[1];
        public Uri[] ThumbnailImages => _thumbnailImages;
        public Uri ThumbnailImage
        {
            get => _thumbnailImages[0];
            set => _thumbnailImages[0] = value;
        }

        
        public PlaylistId PlaylistId { get; }

        Dictionary<VideoId, PlaylistItemEntity> _itemEntityMap = new Dictionary<VideoId, PlaylistItemEntity>();

        public void AddPlaylistItem(VideoId videoId)
        {
            var entity = _playlistRepository.AddItem(PlaylistId.Id, videoId);

            var message = new PlaylistItemAddedMessage(new()
            {
                PlaylistId = PlaylistId,
                AddedItem = videoId
            });

            _itemEntityMap.Add(videoId, entity);

            _messenger.Send(message);
            _messenger.Send(message, videoId);
            _messenger.Send(message, PlaylistId);

            Count = entity.Index;

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, videoId));
        }

        public void UpdateThumbnailImage(Uri thumbnailImage)
        {
            this.ThumbnailImage = thumbnailImage;
            UpdatePlaylistInfo();
        }


        public void AddPlaylistItem(IEnumerable<VideoId> items)
        {
            foreach (var videoId in items)
            {
                AddPlaylistItem(videoId);
            }
        }

        public bool RemovePlaylistItem(VideoId videoId)
        {
            _itemEntityMap.Remove(videoId, out var entity);
            Guard.IsNotNull(entity, nameof(entity));
            var result = _playlistRepository.DeleteItem(PlaylistId.Id, videoId);
            Guard.IsTrue(result, "LocalMylistRepository.DeleteItem");

            var message = new PlaylistItemRemovedMessage(new()
            {
                PlaylistId = PlaylistId,
                RemovedItem = videoId,
            });
            _messenger.Send(message);
            _messenger.Send(message, videoId);
            _messenger.Send(message, PlaylistId.Id);

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, videoId));

            Count = _playlistRepository.GetCount(PlaylistId.Id);
            return result;
        }

        public void RemovePlaylistItems(IEnumerable<VideoId> items)
        {
            foreach (var id in items)
            {
                RemovePlaylistItem(id);
            }
        }



        private void UpdatePlaylistInfo()
        {
            _playlistRepository.UpsertPlaylist(new PlaylistEntity()
            {
                Id = PlaylistId.Id,
                Label = this.Name,
                Count = Count,
                PlaylistOrigin = PlaylistItemsSourceOrigin.Local,
                ThumbnailImage = this.ThumbnailImage,
            });
        }

        int IUserManagedPlaylist.TotalCount => Count;

        public int OneTimeItemsCount => 500;

        LocalPlaylistSortOptions _SortOptions;
        public LocalPlaylistSortOptions SortOptions
        {
            get => _SortOptions ??= new LocalPlaylistSortOptions();
            set => _SortOptions = value;
        }

        IPlaylistSortOptions IPlaylist.SortOptions 
        {
            get => SortOptions;
            set => SortOptions = (LocalPlaylistSortOptions)value;
        }

        public int IndexOf(IVideoContent video)
        {
            var videoId = video.VideoId.ToString();
            return SortedItems.FindIndex(x => x.ContentId == videoId);
        }

        public bool Contains(IVideoContent video)
        {
            var videoId = video.VideoId.ToString();
            return SortedItems.Any(x => x.ContentId == videoId);
        }

        List<PlaylistItemEntity> _sortedItems;
        List<PlaylistItemEntity> SortedItems => _sortedItems ??= _playlistRepository.GetItems(PlaylistId.Id);
        bool _isRequireSort = true;

        public async Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            if (_isRequireSort)
            {
                // 
            }

            var start = pageIndex * pageSize;
            var nicoVideos = await _nicoVideoProvider.GetCachedVideoInfoItemsAsync(SortedItems.Skip(start).Take(pageSize).Select(x => (VideoId)x.ContentId), cancellationToken);
            return nicoVideos;
        }
    }
    
    
}
