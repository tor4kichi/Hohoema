using Hohoema.Models.Domain.LocalMylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Infrastructure;
using I18NPortable;
using LiteDB;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Playlist
{

    public readonly struct PlaylistItemAddedMessageData
    {
        public PlaylistId PlaylistId { get; init; }
        public VideoId AddedItem { get; init; }
        public int Index { get; init; }
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
        public VideoId RemovedItem { get; init; }
        public int Index { get; init; }
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


    public class QueuePlaylistItem : IVideoContent
    {
        private QueuePlaylistItem(QueuePlaylistItem item) { }

        public QueuePlaylistItem() { }

        public QueuePlaylistItem(IVideoContent video)
        {
            Id = video.VideoId; 
            Length = video.Length; 
            Title = video.Title;
            PostedAt = video.PostedAt;
            ThumbnailUrl = video.ThumbnailUrl;
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


    public record LocalPlaylistSortOptions : IPlaylistSortOptions
    {
        public string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }

        public static LocalPlaylistSortOptions Deserialize(string serializedText)
        {
            return System.Text.Json.JsonSerializer.Deserialize<LocalPlaylistSortOptions>(serializedText);
        }
    }

    public class QueuePlaylist : ReadOnlyObservableCollection<QueuePlaylistItem>, IUserManagedPlaylist
    {
        public static readonly PlaylistId Id = new PlaylistId() { Id = "@view", Origin = PlaylistItemsSourceOrigin.Local };


        private readonly IMessenger _messenger;
        private readonly QueuePlaylistRepository _queuePlaylistRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly Dictionary<VideoId, QueuePlaylistItem> _itemEntityMap;

        public int TotalCount => _itemEntityMap.Count;

        public string Name { get; } = Id.Id.Translate();

        public PlaylistId PlaylistId => Id;

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

        public QueuePlaylist(
            IMessenger messenger,
            QueuePlaylistRepository queuePlaylistRepository
            )
            : base(new ObservableCollection<QueuePlaylistItem>(GetQueuePlaylistItems(queuePlaylistRepository, out var itemEntityMap)))
        {
            _itemEntityMap = itemEntityMap;
            _messenger = messenger;
            _queuePlaylistRepository = queuePlaylistRepository;
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
            var message = new PlaylistItemAddedMessage(new() { AddedItem = item.VideoId, Index = item.Index, PlaylistId = Id });
            _messenger.Send(message);
            _messenger.Send(message, item.VideoId);
            _messenger.Send(message, Id);
        }

        private void SendRemovedMessage(in int index, in VideoId videoId)
        {
#if DEBUG
            Guard.IsNotEqualTo(videoId, default(VideoId), "invalid videoId");
#endif
            var message = new PlaylistItemRemovedMessage(new() { RemovedItem = videoId, Index = index, PlaylistId = Id });
            _messenger.Send(message);
            _messenger.Send(message, videoId);
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
            _itemEntityMap.Add(video.VideoId, entityAddedItem);
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

        public QueuePlaylistItem Add(IVideoContent video)
        {
            Guard.IsFalse(Contains(video.VideoId), "already contain videoId");

            var addedItem = new QueuePlaylistItem(video);
            base.Items.Add(addedItem);
            SendAddedMessage(addedItem);
            AddEntity(addedItem);
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IUserManagedPlaylist.TotalCount)));

            return addedItem;
        }

        public QueuePlaylistItem Insert(int index, IVideoContent video)
        {
            Guard.IsFalse(Contains(video.VideoId), "already contain videoId");

            var addedItem = new QueuePlaylistItem(video);
            base.Items.Insert(index, addedItem);
            SendAddedMessage(addedItem);
            AddEntity(addedItem);
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IUserManagedPlaylist.TotalCount)));

            index++;
            foreach (var item in base.Items.Skip(index))
            {
                item.Index = index++;
                SendIndexUpdatedMessage(item.Index, item.VideoId);
                UpdateEntity(item.Index, item.VideoId);
            }

            return addedItem;
        }

        public void Remove(VideoId videoId)
        {
            var item = base.Items.FirstOrDefault(x => x.VideoId == videoId);
            if (item == null) { return; }
            Remove(item);
        }
        public void Remove(IVideoContent removeItem)
        {
            Guard.IsTrue(Contains(removeItem.VideoId), "no contain videoId");

            var item = base.Items.FirstOrDefault(x => x.Equals(removeItem));
            base.Items.Remove(item);
            SendRemovedMessage(item.Index, removeItem.VideoId);
            RemoveEntity(removeItem.VideoId);
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IUserManagedPlaylist.TotalCount)));

            // 他アイテムのIndex更新は必要ない
            // アプリ復帰時に順序が保たれていれば十分
        }


        public bool Contains(in VideoId id)
        {
            return _itemEntityMap.ContainsKey(id);
        }

        public int IndexOf(VideoId id)
        {
            var item = base.Items.FirstOrDefault(x => x.VideoId == id);
            return base.Items.IndexOf(item);
        }

        public int IndexOf(IVideoContent video)
        {
            return IndexOf(video.VideoId);
        }

        public bool Contains(IVideoContent video)
        {
            return Contains(video.VideoId);
        }

        public Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            var start = pageIndex * pageSize;
            return Task.FromResult(this.Skip(start).Take(pageSize).Cast<IVideoContent>());
        }
    }
}
