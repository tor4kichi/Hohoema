using I18NPortable;
using Microsoft.Toolkit.Uwp.Helpers;

using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using CommunityToolkit.Mvvm.Input;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Application;
using Hohoema.Services;
using Hohoema.Models.LocalMylist;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Hohoema.Services.Hohoema.LocalMylist
{
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
            INotificationService notificationService
            )
        {
            _logger = logger;
            _playlistRepository = playlistRepository;
            _nicoVideoProvider = nicoVideoProvider;
            _notificationService = notificationService;

            _playlists = new ObservableCollection<LocalPlaylist>();
            LocalPlaylists = new ReadOnlyObservableCollection<LocalPlaylist>(_playlists);


            WeakReferenceMessenger.Default.Register<SettingsRestoredMessage>(this);

            Load();
        }

        private void Load()
        {
            foreach (var localMylist in _playlists)
            {
                RemoveHandleItemsChanged(localMylist);
            }

            _playlists.Clear();
            _playlistIdToEntity.Clear();

            var localPlaylistEntities = _playlistRepository.GetPlaylistsFromOrigin(PlaylistItemsSourceOrigin.Local);
            var localPlaylists = localPlaylistEntities.Select(x => new LocalPlaylist(x.Id, x.Label, _playlistRepository, _nicoVideoProvider, WeakReferenceMessenger.Default)
            {
                ThumbnailImage = x.ThumbnailImage,
            }).ToList();

            localPlaylists.ForEach(HandleItemsChanged);

            foreach (var entity in localPlaylistEntities)
            {
                _playlistIdToEntity.Add(entity.Id, entity);
            }

            foreach (var i in localPlaylists)
            {
                _playlists.Add(i);
            }
        }

        private readonly ILogger _logger;
        private readonly LocalMylistRepository _playlistRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly INotificationService _notificationService;
        ObservableCollection<LocalPlaylist> _playlists;
        public ReadOnlyObservableCollection<LocalPlaylist> LocalPlaylists { get; }

        Dictionary<string, PlaylistEntity> _playlistIdToEntity = new Dictionary<string, PlaylistEntity>();

        public void Dispose()
        {
            foreach (var playlist in _playlists)
            {
                RemoveHandleItemsChanged(playlist);
            }
        }

        public LocalPlaylist CreatePlaylist(string label)
        {
            var playlist = CreatePlaylist_Internal(label);

            return playlist;
        }

        private LocalPlaylist CreatePlaylist_Internal(string label)
        {
            var entity = new PlaylistEntity()
            {
                Id = LiteDB.ObjectId.NewObjectId().ToString(),
                Label = label,
                PlaylistOrigin = PlaylistItemsSourceOrigin.Local
            };
            _playlistRepository.UpsertPlaylist(entity);
            _playlistIdToEntity.Add(entity.Id, entity);

            var playlist = new LocalPlaylist(entity.Id, label, _playlistRepository, _nicoVideoProvider, WeakReferenceMessenger.Default);

            HandleItemsChanged(playlist);

            _playlists.Add(playlist);            
            return playlist;
        }


        void HandleItemsChanged(LocalPlaylist playlist)
        {
            WeakReferenceMessenger.Default.Register<LocalPlaylist, PlaylistItemAddedMessage, PlaylistId>(playlist, playlist.PlaylistId, (r, m) => 
            {
                var sender = r;
                _notificationService.ShowLiteInAppNotification_Success("InAppNotification_LocalPlaylistAddedItems".Translate(sender.Name, m.Value.AddedItems.Count()));
            });

            WeakReferenceMessenger.Default.Register<LocalPlaylist, PlaylistItemRemovedMessage, PlaylistId>(playlist, playlist.PlaylistId, (r, m) =>
            {
                var sender = r;
                _notificationService.ShowLiteInAppNotification_Success("InAppNotification_LocalPlaylistRemovedItems".Translate(sender.Name, m.Value.RemovedItems.Count()));
            });
        }

        void RemoveHandleItemsChanged(LocalPlaylist playlist)
        {
            WeakReferenceMessenger.Default.Unregister<PlaylistItemAddedMessage, PlaylistId>(playlist, playlist.PlaylistId);
            WeakReferenceMessenger.Default.Unregister<PlaylistItemRemovedMessage, PlaylistId>(playlist, playlist.PlaylistId);
        }


        public LocalPlaylist CreatePlaylist(string label, IEnumerable<IVideoContent> firstItems)
        {
            var playlist = CreatePlaylist(label);
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
            var result = _playlistRepository.DeletePlaylist(localPlaylist.PlaylistId.Id);
            RemoveHandleItemsChanged(localPlaylist);
            return _playlists.Remove(localPlaylist);
        }



        private RelayCommand<LocalPlaylist> _RemoveCommand;
        public RelayCommand<LocalPlaylist> RemoveCommand => _RemoveCommand
            ?? (_RemoveCommand = new RelayCommand<LocalPlaylist>((group) =>
            {
                try
                {
                    RemovePlaylist(group);
                }
                catch (Exception e)
                {
                    _logger.ZLogError(e.ToString());
                }
            }
            , (p) => p != null && LocalPlaylists.Contains(p)
            ));




        
    }
}
