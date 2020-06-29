using I18NPortable;
using Microsoft.Toolkit.Uwp.Helpers;
using Hohoema.Database.Local.LocalMylist;
using Hohoema.Interfaces;
using Hohoema.Models.Helpers;
using Hohoema.Models.LocalMylist;
using Hohoema.Services;
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
using Hohoema.Models.Repository.Playlist;
using Hohoema.Models.Repository;
using Hohoema.Models.Repository.Niconico.NicoVideo;
using Hohoema.UseCase.Services;
using Hohoema.UseCase.Events;

namespace Hohoema.UseCase.Playlist
{
    

    public sealed class LocalMylistManager : IDisposable
    {
        static void MigrateLocalMylistToPlaylistRepository(PlaylistRepository playlistRepository)
        {
            var groups = LocalMylistDb.GetLocalMylistGroups();
            if (groups.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("start migrating LocalMylist.");
                foreach (var legacyLocalPlaylist in groups)
                {
                    var newPlaylist = new PlaylistEntity()
                    {
                        Id = LiteDB.ObjectId.NewObjectId().ToString(),
                        Label = legacyLocalPlaylist.Label,
                        Count = legacyLocalPlaylist.Items.Count,
                        PlaylistOrigin = PlaylistOrigin.Local
                    };

                    playlistRepository.Upsert(newPlaylist);
                    playlistRepository.AddItems(newPlaylist.Id, legacyLocalPlaylist.Items);

                    LocalMylistDb.Remove(legacyLocalPlaylist);

                    System.Diagnostics.Debug.WriteLine($"migrated: {newPlaylist.Label} ({newPlaylist.Count})");
                }
                System.Diagnostics.Debug.WriteLine("migrating LocalMylist done.");
            }
        }

        public LocalMylistManager(
            PlaylistRepository playlistRepository,
            NicoVideoProvider nicoVideoProvider,
            IInAppNotificationService notificationService
            )
        {
            _playlistRepository = playlistRepository;
            _nicoVideoProvider = nicoVideoProvider;
            _notificationService = notificationService;
            MigrateLocalMylistToPlaylistRepository(_playlistRepository);

            var localPlaylistEntities = _playlistRepository.GetPlaylistsFromOrigin(PlaylistOrigin.Local);
            var localPlaylists = localPlaylistEntities.Select(x => new LocalPlaylist(x.Id, _playlistRepository) 
            {
                Label = x.Label,
                Count = x.Count
            }).ToList();

            _playlists = new ObservableCollection<LocalPlaylist>(localPlaylists);
            LocalPlaylists = new ReadOnlyObservableCollection<LocalPlaylist>(_playlists);

            localPlaylists.ForEach(HandleItemsChanged);

            foreach (var entity in localPlaylistEntities)
            {
                _playlistIdToEntity.Add(entity.Id, entity);
            }

        }

        private readonly PlaylistRepository _playlistRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly IInAppNotificationService _notificationService;
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
            _playlistRepository.Upsert(entity);
            _playlistIdToEntity.Add(entity.Id, entity);

            var playlist = new LocalPlaylist(entity.Id, _playlistRepository)
            { 
                Label = label,
            };

            HandleItemsChanged(playlist);

            _playlists.Add(playlist);            
            return playlist;
        }


        void HandleItemsChanged(LocalPlaylist playlist)
        {
            CompositeDisposable disposables = new CompositeDisposable();
            LocalMylistPropertyChangedObserverMap.Add(playlist.Id, disposables);

            Observable.FromEventPattern<LocalPlaylistItemAddedEventArgs>(
                h => playlist.ItemAdded += h,
                h => playlist.ItemAdded -= h
                )
                .Subscribe(args =>
                {
                    var sender = args.Sender as LocalPlaylist;
                    _notificationService.ShowInAppNotification(new InAppNotificationPayload()
                    {
                        Content = "InAppNotification_LocalPlaylistAddedItems".Translate(sender.Label, args.EventArgs.AddedItems.Count)
                    });
                })
                .AddTo(disposables);

            Observable.FromEventPattern<LocalPlaylistItemRemovedEventArgs>(
                h => playlist.ItemRemoved += h,
                h => playlist.ItemRemoved -= h
                )
                .Subscribe(args =>
                {
                    var sender = args.Sender as LocalPlaylist;
                    _notificationService.ShowInAppNotification(new InAppNotificationPayload()
                    {
                        Content = "InAppNotification_LocalPlaylistRemovedItems".Translate(sender.Label, args.EventArgs.RemovedItems.Count)
                    });
                })
                .AddTo(disposables);
        }

        void RemoveHandleItemsChanged(LocalPlaylist playlist)
        {
            if (LocalMylistPropertyChangedObserverMap.Remove(playlist.Id, out var disposables))
            {
                disposables.Dispose();
            }
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
            var result = _playlistRepository.Delete(localPlaylist.Id);
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
