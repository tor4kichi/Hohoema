using I18NPortable;
using Microsoft.Toolkit.Uwp.Helpers;

using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Presentation.Services;
using Prism.Commands;
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
using Microsoft.Toolkit.Mvvm.Messaging;
using Hohoema.Models.Domain.Application;

namespace Hohoema.Models.UseCase.NicoVideos
{
    public sealed class LocalMylistManager : IDisposable, IRecipient<SettingsRestoredMessage>
    {
        void IRecipient<SettingsRestoredMessage>.Receive(SettingsRestoredMessage message)
        {
            Load();
        }


        public LocalMylistManager(
            PlaylistRepository playlistRepository,
            NicoVideoProvider nicoVideoProvider,
            NicoVideoCacheRepository nicoVideoRepository,
            NotificationService notificationService
            )
        {
            _playlistRepository = playlistRepository;
            _nicoVideoProvider = nicoVideoProvider;
            _nicoVideoRepository = nicoVideoRepository;
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

            var localPlaylistEntities = _playlistRepository.GetPlaylistsFromOrigin(PlaylistOrigin.Local);
            var localPlaylists = localPlaylistEntities.Select(x => new LocalPlaylist(x.Id, x.Label, _playlistRepository, _nicoVideoRepository, WeakReferenceMessenger.Default)
            {
                Count = _playlistRepository.GetCount(x.Id),
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


        private readonly PlaylistRepository _playlistRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly NicoVideoCacheRepository _nicoVideoRepository;
        private readonly NotificationService _notificationService;
        ObservableCollection<LocalPlaylist> _playlists;
        public ReadOnlyObservableCollection<LocalPlaylist> LocalPlaylists { get; }

        Dictionary<string, CompositeDisposable> LocalMylistPropertyChangedObserverMap = new Dictionary<string, CompositeDisposable>();

        Dictionary<string, PlaylistEntity> _playlistIdToEntity = new Dictionary<string, PlaylistEntity>();

        public void Dispose()
        {
            foreach (var disposer in LocalMylistPropertyChangedObserverMap.Values)
            {
                disposer.Dispose();
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
                Count = 0,
                Label = label,
                PlaylistOrigin = PlaylistOrigin.Local
            };
            _playlistRepository.UpsertPlaylist(entity);
            _playlistIdToEntity.Add(entity.Id, entity);

            var playlist = new LocalPlaylist(entity.Id, label, _playlistRepository, _nicoVideoRepository, WeakReferenceMessenger.Default);

            HandleItemsChanged(playlist);

            _playlists.Add(playlist);            
            return playlist;
        }


        void HandleItemsChanged(LocalPlaylist playlist)
        {
            CompositeDisposable disposables = new CompositeDisposable();
            LocalMylistPropertyChangedObserverMap.Add(playlist.Id, disposables);

            WeakReferenceMessenger.Default.Register<LocalPlaylist, LocalPlaylistItemAddedMessage>(playlist, (r, m) => 
            {
                var sender = r;
                _notificationService.ShowLiteInAppNotification_Success("InAppNotification_LocalPlaylistAddedItems".Translate(sender.Label, m.Value.AddedItems.Count));
            });

            WeakReferenceMessenger.Default.Register<LocalPlaylist, LocalPlaylistItemRemovedMessage>(playlist, (r, m) =>
            {
                var sender = r;
                _notificationService.ShowLiteInAppNotification_Success("InAppNotification_LocalPlaylistRemovedItems".Translate(sender.Label, m.Value.RemovedItems.Count));
            });
        }

        void RemoveHandleItemsChanged(LocalPlaylist playlist)
        {
            WeakReferenceMessenger.Default.Unregister<LocalPlaylistItemAddedMessage>(playlist);
            WeakReferenceMessenger.Default.Unregister<LocalPlaylistItemRemovedMessage>(playlist);
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
            return _playlists.FirstOrDefault(x => x.Id == playlistId);
        }

        public bool RemovePlaylist(LocalPlaylist localPlaylist)
        {
            var result = _playlistRepository.DeletePlaylist(localPlaylist.Id);
            RemoveHandleItemsChanged(localPlaylist);
            return _playlists.Remove(localPlaylist);
        }


        private DelegateCommand<string> _AddCommand;
        public DelegateCommand<string> AddCommand => _AddCommand
            ?? (_AddCommand = new DelegateCommand<string>((label) =>
            {
                CreatePlaylist(label);
            }
            , (p) => !string.IsNullOrWhiteSpace(p)
            ));


        private DelegateCommand<LocalPlaylist> _RemoveCommand;
        public DelegateCommand<LocalPlaylist> RemoveCommand => _RemoveCommand
            ?? (_RemoveCommand = new DelegateCommand<LocalPlaylist>((group) =>
            {
                RemovePlaylist(group);
            }
            , (p) => p != null && LocalPlaylists.Contains(p)
            ));




        
    }
}
