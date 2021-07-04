using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Video;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.LocalMylist
{
    public enum LocalMylistSortKey
    {
        AddedAt,
        Title,
        PostedAt,
    }

    public enum LocalMylistSortOrder
    {
        Desc,
        Asc,
    }



    public sealed class LocalPlaylist : FixPrism.BindableBase, IUserManagedPlaylist
    {
        private readonly LocalMylistRepository _playlistRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly IMessenger _messenger;

        public LocalPlaylist(string id, string label, LocalMylistRepository playlistRepository, NicoVideoProvider nicoVideoProvider, IMessenger messenger)
        {
            PlaylistId = new PlaylistId() { Id = id, Origin = PlaylistItemsSourceOrigin.Local };
            _playlistRepository = playlistRepository;
            _nicoVideoProvider = nicoVideoProvider;
            _messenger = messenger;
            _name = label;

            _count = _playlistRepository.GetCount(PlaylistId.Id);

            _playlistEntity = _playlistRepository.GetPlaylist(id) ?? new PlaylistEntity() 
            {
                Id = id,
                PlaylistOrigin = PlaylistItemsSourceOrigin.Local,
            };

            _ItemsSortKey = _playlistEntity.ItemsSortKey;
            _ItemsSortOrder = _playlistEntity.ItemsSortOrder;
            _sortIndex = _playlistEntity.PlaylistSortIndex;
        }

        public PlaylistId PlaylistId { get; }


        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    UpdatePlaylistInfo();
                }
            }
        }

        private int _count;
        public int Count 
        {
            get => _count;
            private set
            {
                if (SetProperty(ref _count, value))
                {
                    UpdatePlaylistInfo();
                    RaisePropertyChanged(nameof(IUserManagedPlaylist.TotalCount));
                }
            }
        }

        private int _sortIndex;
        public int SortIndex
        {
            get => _sortIndex;
            set
            {
                if (SetProperty(ref _sortIndex, value))
                {
                    UpdatePlaylistInfo();
                }
            }
        }
        public Uri[] ThumbnailImages => throw new NotSupportedException();

        private Uri _thumbnailImage;
        public Uri ThumbnailImage
        {
            get => _thumbnailImage;
            set
            {
                if (SetProperty(ref _thumbnailImage, value))
                {
                    UpdatePlaylistInfo();
                }
            }
        }


        private LocalMylistSortKey _ItemsSortKey;
        public LocalMylistSortKey ItemsSortKey
        {
            get { return _ItemsSortKey; }
            private set { SetProperty(ref _ItemsSortKey, value); }
        }

        private LocalMylistSortOrder _ItemsSortOrder;
        public LocalMylistSortOrder ItemsSortOrder
        {
            get { return _ItemsSortOrder; }
            private set { SetProperty(ref _ItemsSortOrder, value); }
        }


        public void SetSortOptions(LocalMylistSortKey sortKey, LocalMylistSortOrder sortOrder)
        {
            ItemsSortKey = sortKey;
            ItemsSortOrder = sortOrder;
            SortOptions = new LocalPlaylistSortOptions() { SortKey = sortKey, SortOrder = sortOrder };
            UpdatePlaylistInfo();
            SortOnCurrentOptions();

            var sortedItems = Items.Select(x => x.Video).ToList();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, sortedItems, 0));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, sortedItems, 0));
        }


        PlaylistEntity _playlistEntity;
        private void UpdatePlaylistInfo()
        {
            _playlistEntity ??= new PlaylistEntity()
            {
                Id = PlaylistId.Id,
                PlaylistOrigin = PlaylistItemsSourceOrigin.Local,
            };

            _playlistEntity.Label = this.Name;
            _playlistEntity.ThumbnailImage = this.ThumbnailImage;
            _playlistEntity.PlaylistSortIndex = SortIndex;
            _playlistEntity.ItemsSortKey = ItemsSortKey;
            _playlistEntity.ItemsSortOrder = ItemsSortOrder;
            _playlistRepository.UpsertPlaylist(_playlistEntity);
        }


        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void AddPlaylistItem(IVideoContent item)
        {
            var entity = _playlistRepository.AddItem(PlaylistId.Id, item.VideoId);
            if (entity is null) { return; }

            var message = new PlaylistItemAddedMessage(new()
            {
                PlaylistId = PlaylistId,
                AddedItems = new[] { item.VideoId }
            });

            if (item is NicoVideo nicovideo)
            {
                Items.Add((entity, nicovideo));
            }
            else
            {
                Items.Add((entity, _nicoVideoProvider.GetCachedVideoInfo(item.VideoId)));
            }

            _messenger.Send(message);
            _messenger.Send(message, item.VideoId);
            _messenger.Send(message, PlaylistId);

            Count = _playlistRepository.GetCount(PlaylistId.Id);

            SortOnCurrentOptions();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, IndexOf(item)));
        }

        private void SortOnCurrentOptions()
        {
            Comparison<(PlaylistItemEntity Entity, NicoVideo Video)> comparison = ItemsSortKey switch
            {
                LocalMylistSortKey.AddedAt => ItemsSortOrder == LocalMylistSortOrder.Asc ? ((PlaylistItemEntity Entity, NicoVideo Video) x, (PlaylistItemEntity Entity, NicoVideo Video) y) => x.Entity.Id - y.Entity.Id : ((PlaylistItemEntity Entity, NicoVideo Video) x, (PlaylistItemEntity Entity, NicoVideo Video) y) => y.Entity.Id - x.Entity.Id,
                LocalMylistSortKey.Title => ItemsSortOrder == LocalMylistSortOrder.Asc ? ((PlaylistItemEntity Entity, NicoVideo Video) x, (PlaylistItemEntity Entity, NicoVideo Video) y) => String.Compare(x.Video.Title, y.Video.Title) : ((PlaylistItemEntity Entity, NicoVideo Video) x, (PlaylistItemEntity Entity, NicoVideo Video) y) => String.Compare(y.Video.Title, x.Video.Title),
                LocalMylistSortKey.PostedAt => ItemsSortOrder == LocalMylistSortOrder.Asc ? ((PlaylistItemEntity Entity, NicoVideo Video) x, (PlaylistItemEntity Entity, NicoVideo Video) y) => DateTime.Compare(x.Video.PostedAt, y.Video.PostedAt) : ((PlaylistItemEntity Entity, NicoVideo Video) x, (PlaylistItemEntity Entity, NicoVideo Video) y) => DateTime.Compare(y.Video.PostedAt, x.Video.PostedAt),
                _ => throw new NotSupportedException(),
            };
            Items.Sort(comparison);
        }

        public void AddPlaylistItem(IEnumerable<IVideoContent> items)
        {
            List<IVideoContent> added = new List<IVideoContent>();
            foreach (var item in items)
            {
                var entity = _playlistRepository.AddItem(PlaylistId.Id, item.VideoId);

                if (entity is null) { continue; }

                added.Add(item);
                if (item is NicoVideo nicovideo)
                {
                    Items.Add((entity, nicovideo));
                }
                else
                {
                    Items.Add((entity, _nicoVideoProvider.GetCachedVideoInfo(item.VideoId)));
                }

                var message = new PlaylistItemAddedMessage(new()
                {
                    PlaylistId = PlaylistId,
                    AddedItems = new[] { item.VideoId }
                });

                _messenger.Send(message);
                _messenger.Send(message, item.VideoId);
            }

            _messenger.Send(new PlaylistItemAddedMessage(new()
            {
                PlaylistId = PlaylistId,
                AddedItems = added.Select(x => x.VideoId),
            }), PlaylistId);

            Count = _playlistRepository.GetCount(PlaylistId.Id);

            // 一旦全部破棄してソートし直した状態で全エンティティを取得する
            // ソート後の順番がitemsとは違う順序になりうるから
            // ソート後の若い順から追加を伝えることで、順序の不整合が起きないようにする
            SortOnCurrentOptions();
            foreach (var item in added.Select(x => (Index: IndexOf(x), Item: x)).OrderBy(x => x.Index))
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item.Item, item.Index));
            }
        }

        public bool RemovePlaylistItem(IVideoContent item)
        {
            var result = _playlistRepository.DeleteItem(PlaylistId.Id, item.VideoId);

            if (!result) { return false; }

            var deleted = Items.FirstOrDefault(x => x.Video.VideoId == item.VideoId);
            var index = IndexOf(item);
            Items.Remove(deleted);

            var message = new PlaylistItemRemovedMessage(new()
            {
                PlaylistId = PlaylistId,
                RemovedItems = new[] { item.VideoId },
            });

            _messenger.Send(message);
            _messenger.Send(message, item.VideoId);
            _messenger.Send(message, PlaylistId);

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));

            Count = _playlistRepository.GetCount(PlaylistId.Id);
            return result;
        }

        public void RemovePlaylistItems(IEnumerable<IVideoContent> items)
        {
            List<(int Index, IVideoContent Video)> deletedItems = new();
            foreach (var item in items)
            {
                var result = _playlistRepository.DeleteItem(PlaylistId.Id, item.VideoId);
                if (!result) { continue; }

                var deleted = Items.FirstOrDefault(x => x.Video.VideoId == item.VideoId);
                int index = IndexOf(item);

                deletedItems.Add((index, item));
                var message = new PlaylistItemRemovedMessage(new()
                {
                    PlaylistId = PlaylistId,
                    RemovedItems = new[] { item.VideoId },
                });

                _messenger.Send(message);
                _messenger.Send(message, item.VideoId);
            }

            _messenger.Send(new PlaylistItemRemovedMessage(new()
            {
                PlaylistId = PlaylistId,
                RemovedItems = deletedItems.Select(x => x.Video.VideoId),
            }), PlaylistId);

            // 追加するときとは逆に最後尾からアイテムを消していくことで不整合を発生させないようにする
            foreach (var item in deletedItems.OrderByDescending(x => x.Index))
            {
                Items.RemoveAt(item.Index);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item.Video, item.Index));
            }

            Count = _playlistRepository.GetCount(PlaylistId.Id);
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


        List<(PlaylistItemEntity Entity, NicoVideo Video)> _Items;
        List<(PlaylistItemEntity Entity, NicoVideo Video)> Items
        {
            get
            {
                if (_Items == null)
                {
                    var items = _playlistRepository.GetItems(PlaylistId.Id);
                    var videos = _nicoVideoProvider.GetCachedVideoInfoItems(items.Select(x => (VideoId)x.ContentId));
                    _Items = new List<(PlaylistItemEntity Entity, NicoVideo Video)>(items.Count);
                    foreach (var i in Enumerable.Range(0, items.Count))
                    {
                        _Items.Add((items.ElementAt(i), videos.ElementAt(i)));
                    }

                    SortOnCurrentOptions();
                }

                return _Items;
            }
        }

        void ClearCachedItems()
        {
            _Items = null;
        }

        public int IndexOf(IVideoContent video)
        {
            var videoId = video.VideoId.ToString();
            return Items.FindIndex(x => x.Entity.ContentId == videoId);
        }

        public bool Contains(IVideoContent video)
        {
            var videoId = video.VideoId.ToString();
            return Items.Any(x => x.Entity.ContentId == videoId);
        }


        public async Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            var start = pageIndex * pageSize;

            // バックアップからローカルマイリストを復帰させた場合にTitleとPostedAtをフィルするための処理
            foreach (var item in Items.Skip(start).Take(pageSize))
            {
                if (string.IsNullOrEmpty(item.Video.Title))
                {
                    Items[Items.IndexOf(item)] = (item.Entity, await _nicoVideoProvider.GetCachedVideoInfoAsync(item.Video.VideoId));
                }
            }
            
            return Items.Skip(start).Take(pageSize).Select(x => x.Video);
        }
    }
    
    
}
