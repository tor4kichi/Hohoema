﻿#nullable enable
#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Services;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.LocalMylist;

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


public record LocalPlaylistSortOption : IPlaylistSortOption
{
    public LocalMylistSortKey SortKey { get; init; }

    public LocalMylistSortOrder SortOrder { get; init; }

    private static string GetLocalizedLabel(LocalMylistSortKey SortKey, LocalMylistSortOrder SortOrder)
    {
        return Ioc.Default.GetRequiredService<ILocalizeService>().Translate($"LocalMylistSortKey.{SortKey}_{SortOrder}");
    }

    private string? _label;
    public string Label => _label ??= GetLocalizedLabel(SortKey, SortOrder);

    public string Serialize()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }

    public static LocalPlaylistSortOption Deserialize(string serializedText)
    {
        return System.Text.Json.JsonSerializer.Deserialize<LocalPlaylistSortOption>(serializedText);
    }

    public bool Equals(IPlaylistSortOption other)
    {
        return other is LocalPlaylistSortOption sortOption && this == sortOption;
    }
}

public sealed class LocalPlaylist : ObservableObject, IUserManagedPlaylist
{
    public static LocalPlaylistSortOption[] SortOptions { get; } = new LocalPlaylistSortOption[]
{
        new LocalPlaylistSortOption() { SortKey = LocalMylistSortKey.AddedAt, SortOrder = LocalMylistSortOrder.Desc },
        new LocalPlaylistSortOption() { SortKey = LocalMylistSortKey.AddedAt, SortOrder = LocalMylistSortOrder.Asc },
        new LocalPlaylistSortOption() { SortKey = LocalMylistSortKey.Title, SortOrder = LocalMylistSortOrder.Desc },
        new LocalPlaylistSortOption() { SortKey = LocalMylistSortKey.Title, SortOrder = LocalMylistSortOrder.Asc },
        new LocalPlaylistSortOption() { SortKey = LocalMylistSortKey.PostedAt, SortOrder = LocalMylistSortOrder.Desc },
        new LocalPlaylistSortOption() { SortKey = LocalMylistSortKey.PostedAt, SortOrder = LocalMylistSortOrder.Asc },
    };
    public static LocalPlaylistSortOption DefaultSortOption => SortOptions[1];

    IPlaylistSortOption[] IPlaylist.SortOptions => SortOptions;

    IPlaylistSortOption IPlaylist.DefaultSortOption => DefaultSortOption;


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
                OnPropertyChanged(nameof(IUserManagedPlaylist.TotalCount));
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
        get => _ItemsSortKey;
        private set => SetProperty(ref _ItemsSortKey, value);
    }

    private LocalMylistSortOrder _ItemsSortOrder;
    public LocalMylistSortOrder ItemsSortOrder
    {
        get => _ItemsSortOrder;
        private set => SetProperty(ref _ItemsSortOrder, value);
    }

    private PlaylistEntity _playlistEntity;
    private void UpdatePlaylistInfo()
    {
        _playlistEntity ??= new PlaylistEntity()
        {
            Id = PlaylistId.Id,
            PlaylistOrigin = PlaylistItemsSourceOrigin.Local,
        };

        _playlistEntity.Label = Name;
        _playlistEntity.ThumbnailImage = ThumbnailImage;
        _playlistEntity.PlaylistSortIndex = SortIndex;
        _playlistEntity.ItemsSortKey = ItemsSortKey;
        _playlistEntity.ItemsSortOrder = ItemsSortOrder;
        _playlistRepository.UpsertPlaylist(_playlistEntity);
    }


 

    public event NotifyCollectionChangedEventHandler CollectionChanged;

    public void AddPlaylistItem(IVideoContent video)
    {
        PlaylistItemEntity entity = _playlistRepository.AddItem(PlaylistId.Id, video.VideoId);
        if (entity is null) { return; }

        PlaylistItemAddedMessage message = new(new()
        {
            PlaylistId = PlaylistId,
            AddedItems = new[] { video }
        });

        _ = _messenger.Send(message);
        _ = _messenger.Send(message, video.VideoId);
        _ = _messenger.Send(message, PlaylistId);

        Count = _playlistRepository.GetCount(PlaylistId.Id);

        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, video, _playlistRepository.GetCount(PlaylistId.Id)));
    }

    private static Comparison<(PlaylistItemEntity Entity, NicoVideo Video)> GetSortComparison(LocalMylistSortKey sortKey, LocalMylistSortOrder sortOrder)
    {
        return sortKey switch
        {
            LocalMylistSortKey.AddedAt => sortOrder == LocalMylistSortOrder.Asc ? ((PlaylistItemEntity Entity, NicoVideo Video) x, (PlaylistItemEntity Entity, NicoVideo Video) y) => x.Entity.Id - y.Entity.Id : ((PlaylistItemEntity Entity, NicoVideo Video) x, (PlaylistItemEntity Entity, NicoVideo Video) y) => y.Entity.Id - x.Entity.Id,
            LocalMylistSortKey.Title => sortOrder == LocalMylistSortOrder.Asc ? ((PlaylistItemEntity Entity, NicoVideo Video) x, (PlaylistItemEntity Entity, NicoVideo Video) y) => string.Compare(x.Video.Title, y.Video.Title) : ((PlaylistItemEntity Entity, NicoVideo Video) x, (PlaylistItemEntity Entity, NicoVideo Video) y) => string.Compare(y.Video.Title, x.Video.Title),
            LocalMylistSortKey.PostedAt => sortOrder == LocalMylistSortOrder.Asc ? ((PlaylistItemEntity Entity, NicoVideo Video) x, (PlaylistItemEntity Entity, NicoVideo Video) y) => DateTime.Compare(x.Video.PostedAt, y.Video.PostedAt) : ((PlaylistItemEntity Entity, NicoVideo Video) x, (PlaylistItemEntity Entity, NicoVideo Video) y) => DateTime.Compare(y.Video.PostedAt, x.Video.PostedAt),
            _ => throw new NotSupportedException(),
        };
    }



    public void AddPlaylistItem(IEnumerable<IVideoContent> items)
    {
        List<(IVideoContent Video, int Index)> added = new();
        int tail = _playlistRepository.GetCount(PlaylistId.Id);
        foreach (IVideoContent video in items)
        {
            PlaylistItemEntity entity = _playlistRepository.AddItem(PlaylistId.Id, video.VideoId);

            if (entity is null) { continue; }


            added.Add((video, tail++));

            PlaylistItemAddedMessage message = new(new()
            {
                PlaylistId = PlaylistId,
                AddedItems = new[] { video }
            });

            _ = _messenger.Send(message);
            _ = _messenger.Send(message, video.VideoId);
        }

        _ = _messenger.Send(new PlaylistItemAddedMessage(new()
        {
            PlaylistId = PlaylistId,
            AddedItems = added.Select(x => x.Video),
        }), PlaylistId);

        Count = _playlistRepository.GetCount(PlaylistId.Id);

        // 一旦全部破棄してソートし直した状態で全エンティティを取得する
        // ソート後の順番がitemsとは違う順序になりうるから
        // ソート後の若い順から追加を伝えることで、順序の不整合が起きないようにする
        foreach ((IVideoContent Video, int Index) item in added.OrderBy(x => x.Index))
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item.Video, item.Index));
        }
    }

    public bool RemovePlaylistItem(PlaylistItemToken item)
    {
        (_, _, IVideoContent video) = item;
        bool result = _playlistRepository.DeleteItem(PlaylistId.Id, video.VideoId);

        if (!result) { return false; }

        PlaylistItemRemovedMessage message = new(new()
        {
            PlaylistId = PlaylistId,
            RemovedItems = new[] { video },
        });

        _ = _messenger.Send(message);
        _ = _messenger.Send(message, video.VideoId);
        _ = _messenger.Send(new PlaylistItemRemovedMessage(new()
        {
            PlaylistId = PlaylistId,
            RemovedItems = new[] { video },
        }), PlaylistId);

        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, video));

        Count = _playlistRepository.GetCount(PlaylistId.Id);
        return result;
    }

    public void RemovePlaylistItems(IEnumerable<PlaylistItemToken> items)
    {
        List<IVideoContent> deletedItems = new();
        foreach (PlaylistItemToken item in items)
        {
            (_, _, IVideoContent video) = item;
            bool result = _playlistRepository.DeleteItem(PlaylistId.Id, video.VideoId);
            if (!result) { continue; }

            deletedItems.Add(video);
            PlaylistItemRemovedMessage message = new(new()
            {
                PlaylistId = PlaylistId,
                RemovedItems = new[] { video },
            });

            _ = _messenger.Send(message);
            _ = _messenger.Send(message, video.VideoId);
        }

        _ = _messenger.Send(new PlaylistItemRemovedMessage(new()
        {
            PlaylistId = PlaylistId,
            RemovedItems = deletedItems,
        }), PlaylistId);

        // 追加するときとは逆に最後尾からアイテムを消していくことで不整合を発生させないようにする
        foreach (IVideoContent item in deletedItems)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        }

        Count = _playlistRepository.GetCount(PlaylistId.Id);
    }

    int ISortablePlaylist.TotalCount => Count;

    public int OneTimeItemsCount => 500;

    public async Task<IEnumerable<IVideoContent>> GetAllItemsAsync(IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
    {
        List<PlaylistItemEntity> items = _playlistRepository.GetItems(PlaylistId.Id);
        List<(PlaylistItemEntity Entity, NicoVideo Video)> resultItems = new();
        int index = 0;
        foreach (PlaylistItemEntity item in items)
        {
            NicoVideo video = await _nicoVideoProvider.GetCachedVideoInfoAsync(item.ContentId, cancellationToken);
            resultItems.Add((items.ElementAt(index), video));
            index++;
        }

        LocalPlaylistSortOption sortOptionImpl = sortOption as LocalPlaylistSortOption;
        resultItems.Sort(GetSortComparison(sortOptionImpl.SortKey, sortOptionImpl.SortOrder));

        return resultItems.Select(x => x.Video);
    }
}
