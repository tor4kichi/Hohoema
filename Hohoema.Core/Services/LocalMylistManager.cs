using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Services;
using Hohoema.Models.Application;
using Hohoema.Models.LocalMylist;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace Hohoema.Services.LocalMylist;

public sealed class LocalMylistManager : IDisposable, IRecipient<SettingsRestoredMessage>
{
    void IRecipient<SettingsRestoredMessage>.Receive(SettingsRestoredMessage message)
    {
        Load();
    }


    public LocalMylistManager(
        ILogger logger,
        LocalMylistRepository playlistRepository,
        NicoVideoProvider nicoVideoProvider,
        INotificationService notificationService,
        ILocalizeService localizeService
        )
    {
        _logger = logger;
        _playlistRepository = playlistRepository;
        _nicoVideoProvider = nicoVideoProvider;
        _notificationService = notificationService;
        _localizeService = localizeService;
        _playlists = new ObservableCollection<LocalPlaylist>();
        LocalPlaylists = new ReadOnlyObservableCollection<LocalPlaylist>(_playlists);


        WeakReferenceMessenger.Default.Register<SettingsRestoredMessage>(this);

        Load();
    }

    private void Load()
    {
        foreach (LocalPlaylist localMylist in _playlists)
        {
            RemoveHandleItemsChanged(localMylist);
        }

        _playlists.Clear();
        _playlistIdToEntity.Clear();

        IEnumerable<PlaylistEntity> localPlaylistEntities = _playlistRepository.GetPlaylistsFromOrigin(PlaylistItemsSourceOrigin.Local);
        List<LocalPlaylist> localPlaylists = localPlaylistEntities.Select(x => new LocalPlaylist(x.Id, x.Label, _playlistRepository, _nicoVideoProvider, WeakReferenceMessenger.Default)
        {
            ThumbnailImage = x.ThumbnailImage,
        }).ToList();

        localPlaylists.ForEach(HandleItemsChanged);

        foreach (PlaylistEntity entity in localPlaylistEntities)
        {
            _playlistIdToEntity.Add(entity.Id, entity);
        }

        foreach (LocalPlaylist i in localPlaylists)
        {
            _playlists.Add(i);
        }
    }

    private readonly ILogger _logger;
    private readonly LocalMylistRepository _playlistRepository;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly INotificationService _notificationService;
    private readonly ILocalizeService _localizeService;
    private readonly ObservableCollection<LocalPlaylist> _playlists;
    public ReadOnlyObservableCollection<LocalPlaylist> LocalPlaylists { get; }

    private readonly Dictionary<string, PlaylistEntity> _playlistIdToEntity = new();

    public void Dispose()
    {
        foreach (LocalPlaylist playlist in _playlists)
        {
            RemoveHandleItemsChanged(playlist);
        }
    }

    public LocalPlaylist CreatePlaylist(string label)
    {
        LocalPlaylist playlist = CreatePlaylist_Internal(label);

        return playlist;
    }

    private LocalPlaylist CreatePlaylist_Internal(string label)
    {
        PlaylistEntity entity = new()
        {
            Id = LiteDB.ObjectId.NewObjectId().ToString(),
            Label = label,
            PlaylistOrigin = PlaylistItemsSourceOrigin.Local
        };
        _playlistRepository.UpsertPlaylist(entity);
        _playlistIdToEntity.Add(entity.Id, entity);

        LocalPlaylist playlist = new(entity.Id, label, _playlistRepository, _nicoVideoProvider, WeakReferenceMessenger.Default);

        HandleItemsChanged(playlist);

        _playlists.Add(playlist);
        return playlist;
    }

    private void HandleItemsChanged(LocalPlaylist playlist)
    {
        WeakReferenceMessenger.Default.Register<LocalPlaylist, PlaylistItemAddedMessage, PlaylistId>(playlist, playlist.PlaylistId, (r, m) =>
        {
            LocalPlaylist sender = r;
            _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("InAppNotification_LocalPlaylistAddedItems", sender.Name, m.Value.AddedItems.Count()));
        });

        WeakReferenceMessenger.Default.Register<LocalPlaylist, PlaylistItemRemovedMessage, PlaylistId>(playlist, playlist.PlaylistId, (r, m) =>
        {
            LocalPlaylist sender = r;
            _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("InAppNotification_LocalPlaylistRemovedItems", sender.Name, m.Value.RemovedItems.Count()));
        });
    }

    private void RemoveHandleItemsChanged(LocalPlaylist playlist)
    {
        WeakReferenceMessenger.Default.Unregister<PlaylistItemAddedMessage, PlaylistId>(playlist, playlist.PlaylistId);
        WeakReferenceMessenger.Default.Unregister<PlaylistItemRemovedMessage, PlaylistId>(playlist, playlist.PlaylistId);
    }


    public LocalPlaylist CreatePlaylist(string label, IEnumerable<IVideoContent> firstItems)
    {
        LocalPlaylist playlist = CreatePlaylist(label);
        playlist.AddPlaylistItem(firstItems);

        return playlist;
    }


    public bool HasPlaylist(string playlistId)
    {
        return _playlistIdToEntity.ContainsKey(playlistId);
    }

    public LocalPlaylist GetPlaylist(string playlistId)
    {
        return _playlists.FirstOrDefault(x => x.PlaylistId.Id == playlistId);
    }

    public bool RemovePlaylist(LocalPlaylist localPlaylist)
    {
        _ = _playlistRepository.DeletePlaylist(localPlaylist.PlaylistId.Id);
        RemoveHandleItemsChanged(localPlaylist);
        return _playlists.Remove(localPlaylist);
    }



    private RelayCommand<LocalPlaylist> _RemoveCommand;
    public RelayCommand<LocalPlaylist> RemoveCommand => _RemoveCommand ??= new RelayCommand<LocalPlaylist>((group) =>
        {
            try
            {
                _ = RemovePlaylist(group);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }
        , (p) => p != null && LocalPlaylists.Contains(p)
        );





}
