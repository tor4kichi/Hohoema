using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Hohoema.Models.Domain.Playlist
{

    public sealed class LocalPlaylistItemRemovedEventArgs
    {
        public string PlaylistId { get; internal set; }
        public IReadOnlyCollection<string> RemovedItems { get; internal set; }
    }

    public sealed class LocalPlaylistItemAddedEventArgs
    {
        public string PlaylistId { get; internal set; }
        public IReadOnlyCollection<string> AddedItems { get; internal set; }
    }

    public sealed class LocalPlaylist : IPlaylist
    {
        private readonly PlaylistRepository _playlistRepository;
        private readonly NicoVideoCacheRepository _nicoVideoRepository;

        internal LocalPlaylist(string id, string label, PlaylistRepository playlistRepository, NicoVideoCacheRepository nicoVideoRepository)
        {
            Id = id;
            Label = label;
            _playlistRepository = playlistRepository;
            _nicoVideoRepository = nicoVideoRepository;
        }

        public string Id { get; }

        public string Label { get; private set; }

        public void UpdateLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) { throw new InvalidOperationException(); }

            if (string.Compare(Label, label) != 0)
            {
                Label = label;
                UpdatePlaylistInfo();
            }
        }

        public int Count { get; set; }

        public int SortIndex { get; set; }

        Uri[] _thumbnailImages = new Uri[1];
        public Uri[] ThumbnailImages => _thumbnailImages;
        public Uri ThumbnailImage
        {
            get => _thumbnailImages[0];
            set => _thumbnailImages[0] = value;
        }

        public event EventHandler<LocalPlaylistItemRemovedEventArgs> ItemRemoved;
        public event EventHandler<LocalPlaylistItemAddedEventArgs> ItemAdded;


        public void AddPlaylistItem(IVideoContent item)
        {
            _playlistRepository.AddItem(Id, item.Id);
            ItemAdded?.Invoke(this, new LocalPlaylistItemAddedEventArgs()
            {
                PlaylistId = Id,
                AddedItems = new[] { item.Id }
            });

            Count = _playlistRepository.GetCount(Id);
            if (Count == 1)
            {
                UpdateThumbnailImage(new Uri(item.ThumbnailUrl));
            }
        }

        public void UpdateThumbnailImage(Uri thumbnailImage)
        {
            this.ThumbnailImage = thumbnailImage;
            UpdatePlaylistInfo();
        }


        public void AddPlaylistItem(IEnumerable<IVideoContent> items)
        {
            var ids = items.Select(x => x.Id).ToList();
            _playlistRepository.AddItems(Id, ids);
            ItemAdded?.Invoke(this, new LocalPlaylistItemAddedEventArgs()
            {
                PlaylistId = Id,
                AddedItems = ids
            });
        }



        public List<NicoVideo> GetPlaylistItems()
        {
            var items = _playlistRepository.GetItems(Id);
            return _nicoVideoRepository.Get(items.Select(x => x.ContentId));
        }

        public bool RemovePlaylistItem(IVideoContent item)
        {
            var result = _playlistRepository.DeleteItem(Id, item.Id);

            if (result)
            {
                ItemRemoved?.Invoke(this, new LocalPlaylistItemRemovedEventArgs()
                {
                    PlaylistId = Id,
                    RemovedItems = new[] { item.Id }
                });
            }

            Count = _playlistRepository.GetCount(Id);

            return result;
        }

        public int RemovePlaylistItems(IEnumerable<IVideoContent> items)
        {
            var ids = items.Select(x => x.Id).ToList();
            var result = _playlistRepository.DeleteItems(Id, ids);

            if (result > 0)
            {
                ItemRemoved?.Invoke(this, new LocalPlaylistItemRemovedEventArgs()
                {
                    PlaylistId = Id,
                    RemovedItems = ids
                });
            }

            Count = _playlistRepository.GetCount(Id);

            return result;
        }



        private void UpdatePlaylistInfo()
        {
            _playlistRepository.UpsertPlaylist(new PlaylistEntity()
            {
                Id = this.Id,
                Label = this.Label,
                Count = 1,
                PlaylistOrigin = PlaylistOrigin.Local,
                ThumbnailImage = this.ThumbnailImage,
            });
        }
    }
    
    
}
