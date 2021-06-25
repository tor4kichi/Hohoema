using Hohoema.Models.Domain.LocalMylist;
using I18NPortable;
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


    public class QueuePlaylist : ReadOnlyObservableCollection<PlaylistItem>, IShufflePlaylistItemsSource
    {
        public static readonly PlaylistId Id = new PlaylistId() { Id = "@view", Origin = PlaylistItemsSourceOrigin.Local };


        private readonly IMessenger _messenger;
        private readonly LocalMylistRepository _playlistRepository;

        private readonly Dictionary<VideoId, PlaylistItemEntity> _itemEntityMap;

        int IShufflePlaylistItemsSource.MaxItemsCount => _itemEntityMap.Count;

        int IPlaylistItemsSource.OneTimeItemsCount => 500;

        string IPlaylist.Name { get; } = Id.Id.Translate();

        PlaylistId IPlaylist.PlaylistId => Id;

        public QueuePlaylist(
            IMessenger messenger,
            LocalMylistRepository playlistRepository)
            : base(new ObservableCollection<PlaylistItem>(GetQueuePlaylistItems(playlistRepository, out var itemEntityMap)))
        {
            _itemEntityMap = itemEntityMap;
            _messenger = messenger;
            _playlistRepository = playlistRepository;
        }

        static IEnumerable<PlaylistItem> GetQueuePlaylistItems(LocalMylistRepository localMylistRepository, out Dictionary<VideoId, PlaylistItemEntity> eneityMap)
        {
            var items = localMylistRepository.GetItems(Id.Id);
            eneityMap = items.ToDictionary(x => (VideoId)x.ContentId);
            return items.Select((x, i) => new PlaylistItem(Id, i, x.ContentId));
        }


        private void SendAddedMessage(in int index, in VideoId videoId)
        {
#if DEBUG
            Guard.IsNotEqualTo(videoId, default(VideoId), "invalid videoId");
#endif
            var message = new PlaylistItemAddedMessage(new() { AddedItem = videoId, Index = index, PlaylistId = Id });
            _messenger.Send(message);
            _messenger.Send(message, videoId);
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

        private void AddEntity(int index, VideoId videoId)
        {
#if DEBUG
            Guard.IsNotEqualTo(videoId, default(VideoId), "invalid videoId");
#endif
            var entityAddedItem = _playlistRepository.AddItem(Id.Id, videoId, index);
            _itemEntityMap.Add(videoId, entityAddedItem);
        }

        private void RemoveEntity(VideoId videoId)
        {
#if DEBUG
            Guard.IsNotEqualTo(videoId, default(VideoId), "invalid videoId");
#endif
            _playlistRepository.DeleteItem(Id.Id, videoId);
            _itemEntityMap.Remove(videoId);
        }

        private void UpdateEntity(int index, VideoId videoId)
        {
#if DEBUG
            Guard.IsNotEqualTo(videoId, default(VideoId), "invalid videoId");
#endif
            var entityItem = _itemEntityMap[videoId];
            entityItem.Index = index;
            _playlistRepository.UpdateItem(entityItem);
        }        

        public PlaylistItem Add(VideoId videoId)
        {
            Guard.IsFalse(Contains(videoId), "already contain videoId");

            var addedItem = new PlaylistItem(Id, base.Items.Count, videoId);
            base.Items.Add(addedItem);
            SendAddedMessage(addedItem.ItemIndex, addedItem.ItemId);
            AddEntity(addedItem.ItemIndex, addedItem.ItemId);
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IShufflePlaylistItemsSource.MaxItemsCount)));

            return addedItem;
        }

        public PlaylistItem Insert(int index, VideoId videoId)
        {
            Guard.IsFalse(Contains(videoId), "already contain videoId");

            var addedItem = new PlaylistItem(Id, index, videoId);
            base.Items.Insert(index, addedItem);
            SendAddedMessage(addedItem.ItemIndex, addedItem.ItemId);
            AddEntity(addedItem.ItemIndex, addedItem.ItemId);
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IShufflePlaylistItemsSource.MaxItemsCount)));

            index++;
            foreach (var item in base.Items.Skip(index))
            {
                item.ItemIndex = index++;
                SendIndexUpdatedMessage(item.ItemIndex, videoId);
                UpdateEntity(item.ItemIndex, item.ItemId);
            }

            return addedItem;
        }

        public void Remove(VideoId videoId)
        {
            var item = base.Items.FirstOrDefault(x => x.ItemId == videoId);
            if (item == null) { return; }
            Remove(item);
        }
        public void Remove(PlaylistItem removeItem)
        {
            Guard.IsTrue(Contains(removeItem.ItemId), "no contain videoId");

            var index = removeItem.ItemIndex;
#if DEBUG
            var realIndex = base.Items.IndexOf(removeItem);
            Guard.IsEqualTo(realIndex, index, "not same index");
#endif
            base.Items.RemoveAt(index);
            SendRemovedMessage(index, removeItem.ItemId);
            RemoveEntity(removeItem.ItemId);
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IShufflePlaylistItemsSource.MaxItemsCount)));

            foreach (var item in base.Items.Skip(index))
            {
                item.ItemIndex = index++;
                SendIndexUpdatedMessage(item.ItemIndex, item.ItemId);
                UpdateEntity(item.ItemIndex, item.ItemId);
            }
        }


        public bool Contains(in VideoId id)
        {
            return _itemEntityMap.ContainsKey(id);
        }

        public int IndexOf(in VideoId id)
        {
            return _itemEntityMap[id].Index;
        }

        ValueTask<IEnumerable<PlaylistItem>> IPlaylistItemsSource.GetRangeAsync(int start, int count, CancellationToken ct)
        {
            return new(this.Skip(start).Take(count));
        }
    }
}
