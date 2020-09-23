using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.Playlist.Commands;
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

        internal LocalPlaylist(string id, PlaylistRepository playlistRepository, NicoVideoCacheRepository nicoVideoRepository)
        {
            Id = id;
            _playlistRepository = playlistRepository;
            _nicoVideoRepository = nicoVideoRepository;
            ItemsAddCommand = new LocalPlaylistAddItemCommand(this);
            ItemsRemoveCommand = new LocalPlaylistRemoveItemCommand(this);
        }

        public LocalPlaylistAddItemCommand ItemsAddCommand { get; }
        public LocalPlaylistRemoveItemCommand ItemsRemoveCommand { get; }

        public string Id { get; }

        public string Label { get; set; }

        public int Count { get; set; }

        public int SortIndex { get; set; }


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



        public IEnumerable<NicoVideo> GetPlaylistItems()
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

            return result;
        }
    }
    
    
}
