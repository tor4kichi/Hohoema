using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Models.Domain.LocalMylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Helpers;
using Hohoema.Models.Infrastructure;
using I18NPortable;
using LiteDB;
using Microsoft.Toolkit.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Video;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;

namespace Hohoema.Models.Domain.Playlist
{

    public readonly struct PlaylistItemAddedMessageData
    {
        public PlaylistId PlaylistId { get; init; }
        public IEnumerable<IVideoContent> AddedItems { get; init; }
    }

    public sealed class PlaylistItemAddedMessage : ValueChangedMessage<PlaylistItemAddedMessageData>
    {
        public PlaylistItemAddedMessage(PlaylistItemAddedMessageData value) : base(value)
        {
        }
    }


    public readonly struct PlaylistItemRemovedMessageData
    {
        public PlaylistId PlaylistId { get; init; }
        public IEnumerable<IVideoContent> RemovedItems { get; init; }
    }

    public sealed class PlaylistItemRemovedMessage : ValueChangedMessage<PlaylistItemRemovedMessageData>
    {
        public PlaylistItemRemovedMessage(PlaylistItemRemovedMessageData value) : base(value)
        {
        }
    }


    public readonly struct PlaylistItemIndexUpdatedMessageData
    {
        public PlaylistId PlaylistId { get; init; }
        public VideoId ContentId { get; init; }
        public int Index { get; init; }
    }

    public sealed class ItemIndexUpdatedMessage : ValueChangedMessage<PlaylistItemIndexUpdatedMessageData>
    {
        public ItemIndexUpdatedMessage(PlaylistItemIndexUpdatedMessageData value) : base(value)
        {
        }
    }

    public class QueuePlaylistItem : IVideoContent, IVideoContentProvider
    {
        private QueuePlaylistItem(QueuePlaylistItem item) { }

        public QueuePlaylistItem() { }

        public QueuePlaylistItem(IVideoContent video, IVideoContentProvider videoContentProvider, PlaylistId sourcePlaylistId)
        {
            Id = video.VideoId; 
            Length = video.Length; 
            Title = video.Title;
            PostedAt = video.PostedAt;
            ThumbnailUrl = video.ThumbnailUrl;
            ProviderId = videoContentProvider.ProviderId;
            ProviderType = videoContentProvider.ProviderType;
            SourcePlaylistId = sourcePlaylistId;
        }

        [BsonId]
        public string Id { get; init; }

        VideoId? _videoId;
        public VideoId VideoId => _videoId ??= Id;

        public TimeSpan Length { get; init; }

        public string ThumbnailUrl { get; init; }

        public DateTime PostedAt { get; init; }

        public string Title { get; init; }

        public int Index { get; internal set; }

        public string ProviderId { get; init; }

        public OwnerType ProviderType { get; init; }

        public PlaylistId SourcePlaylistId { get; init; }

        public bool Equals(IVideoContent other)
        {
            return this.VideoId == other.VideoId;
        }
    }

    public class QueuePlaylistRepository : LiteDBServiceBase<QueuePlaylistItem>
    {
        public QueuePlaylistRepository(LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }
    }


    public record QueuePlaylistSortOption : IPlaylistSortOption
    {
        public LocalMylistSortKey SortKey { get; init; }

        public LocalMylistSortOrder SortOrder { get; init; }
        
        string? _label;
        public string Label => _label ??= $"LocalMylistSortKey.{SortKey}_{SortOrder}".Translate();

        public string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }

        public static QueuePlaylistSortOption Deserialize(string serializedText)
        {
            return System.Text.Json.JsonSerializer.Deserialize<QueuePlaylistSortOption>(serializedText);
        }

        public bool Equals(IPlaylistSortOption other)
        {
            return other is QueuePlaylistSortOption sortOption ? this == sortOption : false;
        }
    }

    public class QueuePlaylist : ObservableObject, IReadOnlyCollection<QueuePlaylistItem>, INotifyCollectionChanged, IUserManagedPlaylist
    {
        public static QueuePlaylistSortOption[] SortOptions { get; } = new QueuePlaylistSortOption[]
{
            new QueuePlaylistSortOption() { SortKey = LocalMylistSortKey.AddedAt, SortOrder = LocalMylistSortOrder.Desc },
            new QueuePlaylistSortOption() { SortKey = LocalMylistSortKey.AddedAt, SortOrder = LocalMylistSortOrder.Asc },
            new QueuePlaylistSortOption() { SortKey = LocalMylistSortKey.Title, SortOrder = LocalMylistSortOrder.Desc },
            new QueuePlaylistSortOption() { SortKey = LocalMylistSortKey.Title, SortOrder = LocalMylistSortOrder.Asc },
            new QueuePlaylistSortOption() { SortKey = LocalMylistSortKey.PostedAt, SortOrder = LocalMylistSortOrder.Desc },
            new QueuePlaylistSortOption() { SortKey = LocalMylistSortKey.PostedAt, SortOrder = LocalMylistSortOrder.Asc },
        };

        public static QueuePlaylistSortOption DefaultSortOption => SortOptions[0];

        IPlaylistSortOption[] IPlaylist.SortOptions => SortOptions;

        IPlaylistSortOption IPlaylist.DefaultSortOption => DefaultSortOption;

        public static readonly PlaylistId Id = new PlaylistId() { Id = "@view", Origin = PlaylistItemsSourceOrigin.Local };


        private readonly IMessenger _messenger;
        private readonly IScheduler _scheduler;
        private readonly QueuePlaylistRepository _queuePlaylistRepository;
        private readonly QueuePlaylistSetting _queuePlaylistSetting;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly Dictionary<VideoId, QueuePlaylistItem> _itemEntityMap;

        public int TotalCount => _itemEntityMap.Count;

        public string Name { get; } = Id.Id.Translate();

        public PlaylistId PlaylistId => Id;

        public int Count => ((IReadOnlyCollection<QueuePlaylistItem>)Items).Count;

        ObservableCollection<QueuePlaylistItem> Items;
        public QueuePlaylist(
            IMessenger messenger,
            IScheduler scheduler,
            QueuePlaylistRepository queuePlaylistRepository,
            QueuePlaylistSetting queuePlaylistSetting
            )
        {
            Items = new (GetQueuePlaylistItems(queuePlaylistRepository, out var itemEntityMap));
            _itemEntityMap = itemEntityMap;
            _messenger = messenger;
            _scheduler = scheduler;
            _queuePlaylistRepository = queuePlaylistRepository;
            _queuePlaylistSetting = queuePlaylistSetting;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                ((INotifyCollectionChanged)Items).CollectionChanged += value;
            }

            remove
            {
                ((INotifyCollectionChanged)Items).CollectionChanged -= value;
            }
        }

        static IEnumerable<QueuePlaylistItem> GetQueuePlaylistItems(QueuePlaylistRepository queuePlaylistRepository, out Dictionary<VideoId, QueuePlaylistItem> eneityMap)
        {
            var items = queuePlaylistRepository.ReadAllItems();
            eneityMap = items.ToDictionary(x => x.VideoId);
            items.Sort((a, b) => a.Index - b.Index);
            return items;
        }


        private void SendAddedMessage(QueuePlaylistItem item)
        {
#if DEBUG
            Guard.IsNotEqualTo(item.VideoId, default(VideoId), "invalid videoId");
#endif
            var message = new PlaylistItemAddedMessage(new() { AddedItems = new[] { item }, PlaylistId = Id });
            _messenger.Send(message);
            _messenger.Send(message, item.VideoId);
            _messenger.Send(message, Id);
        }

        private void SendRemovedMessage(in int index, QueuePlaylistItem item)
        {
#if DEBUG
            Guard.IsNotEqualTo(item.VideoId, default(VideoId), "invalid videoId");
#endif
            var message = new PlaylistItemRemovedMessage(new() { RemovedItems = new[] { item }, PlaylistId = Id });
            _messenger.Send(message);
            _messenger.Send(message, item.VideoId);
            _messenger.Send(message, Id);
        }

        private void SendIndexUpdatedMessage(in int index, in VideoId videoId)
        {
#if DEBUG
            Guard.IsNotEqualTo(videoId, default(VideoId), "invalid videoId");
#endif
            var message = new ItemIndexUpdatedMessage(new() { ContentId = videoId, Index = index, PlaylistId = Id });
            _messenger.Send(message);
            _messenger.Send(message, videoId);
            _messenger.Send(message, Id);
        }

        private void AddEntity(QueuePlaylistItem video)
        {
#if DEBUG
            Guard.IsNotEqualTo(video.VideoId, default(VideoId), "invalid videoId");
#endif
            var entityAddedItem = _queuePlaylistRepository.CreateItem(video);
            _itemEntityMap.Add(video.VideoId, video);
        }

        private void RemoveEntity(VideoId videoId)
        {
#if DEBUG
            Guard.IsNotEqualTo(videoId, default(VideoId), "invalid videoId");
#endif
            _queuePlaylistRepository.DeleteItem(videoId.ToString());
            _itemEntityMap.Remove(videoId);
        }

        private void UpdateEntity(int index, VideoId videoId)
        {
#if DEBUG
            Guard.IsNotEqualTo(videoId, default(VideoId), "invalid videoId");
#endif
            var entityItem = _itemEntityMap[videoId];
            entityItem.Index = index;
            _queuePlaylistRepository.UpdateItem(entityItem);
        }        

        public QueuePlaylistItem Add(IVideoContent video, PlaylistId playlistId = null)
        {
            Guard.IsAssignableToType<IVideoContentProvider>(video, nameof(video));
            Guard.IsFalse(Contains(video.VideoId), "already contain videoId");

            var addedItem = new QueuePlaylistItem(video, video as IVideoContentProvider, playlistId);            
            Items.Add(addedItem);
            SendAddedMessage(addedItem);
            AddEntity(addedItem);
            OnPropertyChanged(nameof(IUserManagedPlaylist.TotalCount));

            return addedItem;
        }

        public QueuePlaylistItem Insert(int index, IVideoContent video, PlaylistId playlistId = null)
        {
            Guard.IsAssignableToType<IVideoContentProvider>(video, nameof(video));
            Guard.IsFalse(Contains(video.VideoId), "already contain videoId");

            var addedItem = new QueuePlaylistItem(video, video as IVideoContentProvider, playlistId);
            Items.Insert(index, addedItem);
            SendAddedMessage(addedItem);
            AddEntity(addedItem);
            OnPropertyChanged(nameof(IUserManagedPlaylist.TotalCount));

            index++;
            foreach (var item in Items.Skip(index))
            {
                item.Index = index++;
                SendIndexUpdatedMessage(item.Index, item.VideoId);
                UpdateEntity(item.Index, item.VideoId);
            }

            return addedItem;
        }

        public void Remove(VideoId videoId)
        {
            var item = Items.FirstOrDefault(x => x.VideoId == videoId);
            if (item == null) { return; }
            Remove(item);
        }
        public void Remove(IVideoContent removeItem)
        {
            Guard.IsTrue(Contains(removeItem.VideoId), "no contain videoId");

            _scheduler.Schedule(() => 
            {
                var item = Items.FirstOrDefault(x => x.Equals(removeItem));
                if (item == null) { return; }

                Items.Remove(item);
                SendRemovedMessage(item.Index, item);
                RemoveEntity(removeItem.VideoId);
                OnPropertyChanged(nameof(IUserManagedPlaylist.TotalCount));               
            });

            // 他アイテムのIndex更新は必要ない
            // アプリ復帰時に順序が保たれていれば十分

            OnPropertyChanged(nameof(TotalCount));
        }


        public bool Contains(in VideoId id)
        {
            return _itemEntityMap.ContainsKey(id);
        }

        public int IndexOf(VideoId id)
        {
            var item = Items.FirstOrDefault(x => x.VideoId == id);
            return Items.IndexOf(item);
        }

        public int IndexOf(IVideoContent video)
        {
            return IndexOf(video.VideoId);
        }

        public bool Contains(IVideoContent video)
        {
            return Contains(video.VideoId);
        }

        public Task<IEnumerable<IVideoContent>> GetAllItemsAsync(IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
        {
            var items = this.ToList();
            var sort = sortOption as QueuePlaylistSortOption;

            if (_queuePlaylistSetting.IsGroupingNearByTitleThenByTitleAscending)
            {
                var titleSimulalityThreshold = _queuePlaylistSetting.TitleSimulalityThreshold;
                // タイトルの類似度ごとにグループ化し
                Dictionary<QueuePlaylistItem, List<QueuePlaylistItem>> groupedByTitleSimulality = new();

                items.Sort(GetSortComparison(sort.SortKey, sort.SortOrder));

                foreach (var item in items)
                {
                    QueuePlaylistItem groupKey = null;
                    foreach (var key in groupedByTitleSimulality.Keys)
                    {
                        if (StringHelper.CalculateSimilarity(item.Title, key.Title) > titleSimulalityThreshold)
                        {
                            groupKey = key;
                            break;
                        }
                        else if (key.SourcePlaylistId is not null 
                            && item.SourcePlaylistId == key.SourcePlaylistId)
                        {
                           groupKey = key;
                           break;
                        }
                    }

                    if (groupKey is not null)
                    {
                        groupedByTitleSimulality[groupKey].Add(item);
                    }
                    else
                    {
                        groupedByTitleSimulality.Add(item, new List<QueuePlaylistItem>() { item });
                    }
                }

                // グループ内アイテムを投稿日時 昇順でソート
                {
                    var titleAscComparision = GetSortComparison(LocalMylistSortKey.PostedAt, LocalMylistSortOrder.Asc);
                    foreach (var groupList in groupedByTitleSimulality.Values)
                    {
                        groupList.Sort(titleAscComparision);
                    }
                }

                // 各グループの一番新しく投稿されたっぽいアイテムを代表値としてソート
                List<QueuePlaylistItem> keyList = groupedByTitleSimulality.Keys.ToList();
                keyList.Sort(GetSortComparison(sort.SortKey, sort.SortOrder));

                // グループを全て連結してリスト化
                items = keyList.SelectMany(key => groupedByTitleSimulality[key]).ToList();
            }
            else
            {
                items.Sort(GetSortComparison(sort.SortKey, sort.SortOrder));
            }


            return Task.FromResult(items.Cast<IVideoContent>());
        }


        private static Comparison<QueuePlaylistItem> GetSortComparison(LocalMylistSortKey sortKey, LocalMylistSortOrder sortOrder)
        {
            return sortKey switch
            {
                LocalMylistSortKey.AddedAt => sortOrder == LocalMylistSortOrder.Asc ? (QueuePlaylistItem x, QueuePlaylistItem y) => y.Index - x.Index: (QueuePlaylistItem x, QueuePlaylistItem y) => x.Index - y.Index,
                LocalMylistSortKey.Title => sortOrder == LocalMylistSortOrder.Asc ? (QueuePlaylistItem x, QueuePlaylistItem y) => String.Compare(x.Title, y.Title) : (QueuePlaylistItem x, QueuePlaylistItem y) => String.Compare(y.Title, x.Title),
                LocalMylistSortKey.PostedAt => sortOrder == LocalMylistSortOrder.Asc ? (QueuePlaylistItem x, QueuePlaylistItem y) => DateTime.Compare(x.PostedAt, y.PostedAt) : (QueuePlaylistItem x, QueuePlaylistItem y) => DateTime.Compare(y.PostedAt, x.PostedAt),
                _ => throw new NotSupportedException(),
            };
        }

        public IEnumerator<QueuePlaylistItem> GetEnumerator()
        {
            return ((IEnumerable<QueuePlaylistItem>)Items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Items).GetEnumerator();
        }
    }
}
