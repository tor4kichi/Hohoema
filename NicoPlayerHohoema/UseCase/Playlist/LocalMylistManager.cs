using Microsoft.Toolkit.Uwp.Helpers;
using NicoPlayerHohoema.Database.Local.LocalMylist;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Models.LocalMylist;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Repository.Playlist;
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

namespace NicoPlayerHohoema.UseCase.Playlist
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
                        PlaylistOrigin = Interfaces.PlaylistOrigin.Local
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
            NicoVideoProvider nicoVideoProvider
            )
        {
            _playlistRepository = playlistRepository;
            _nicoVideoProvider = nicoVideoProvider;
            MigrateLocalMylistToPlaylistRepository(_playlistRepository);

            var localPlaylistEntities = _playlistRepository.GetPlaylistsFromOrigin(Interfaces.PlaylistOrigin.Local);
            var localPlaylists = localPlaylistEntities.Select(x => new LocalPlaylist(x.Id, x.Label, x.Count)).ToList();

            _playlists = new ObservableCollection<LocalPlaylist>(localPlaylists);
            LocalPlaylists = new ReadOnlyObservableCollection<LocalPlaylist>(_playlists);

            foreach (var entity in localPlaylistEntities)
            {
                _playlistIdToEntity.Add(entity.Id, entity);
            }

            /*
            LocalPlaylists.CollectionChangedAsObservable()
                .Subscribe(e =>
                {
                    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                    {
                        foreach (var newItem in e.NewItems.Cast<LocalPlaylist>())
                        {
                            CompositeDisposable disposables = new CompositeDisposable();
                            new[]
                            {
                                newItem.ObserveProperty(x => x.Label).ToUnit(),
                                newItem.CollectionChangedAsObservable().ToUnit()
                            }
                            .Merge()
                            .Throttle(TimeSpan.FromSeconds(1))
                            .Subscribe(_ =>
                            {
                                LocalMylistDb.AddOrUpdate(new LocalMylistData()
                                {
                                    Id = newItem.Id,
                                    Label = newItem.Label,
                                    Items = newItem.ToList(),
                                    SortIndex = newItem.SortIndex
                                });
                            })
                            .AddTo(disposables);

                            LocalMylistPropertyChangedObserverMap.Add(newItem.Id, disposables);
                        }
                    }
                    else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                    {
                        foreach (var removeItem in e.NewItems.Cast<LocalMylistGroup>())
                        {
                            LocalMylistDb.Get(removeItem.Id);

                            if (LocalMylistPropertyChangedObserverMap.TryGetValue(removeItem.Id, out var disposer))
                            {
                                disposer.Dispose();
                                LocalMylistPropertyChangedObserverMap.Remove(removeItem.Id);
                            }
                        }
                    }
                });
                */
        }

        private readonly PlaylistRepository _playlistRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;
        ObservableCollection<LocalPlaylist> _playlists;
        public ReadOnlyObservableCollection<LocalPlaylist> LocalPlaylists { get; }

        Dictionary<string, IDisposable> LocalMylistPropertyChangedObserverMap = new Dictionary<string, IDisposable>();

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
            var entity = new PlaylistEntity()
            {
                Id = LiteDB.ObjectId.NewObjectId().ToString(),
                Count = 0,
                Label = label,
                PlaylistOrigin = PlaylistOrigin.Local
            };
            _playlistRepository.Upsert(entity);
            _playlistIdToEntity.Add(entity.Id, entity);

            var playlist = new LocalPlaylist(entity.Id, entity.Label, entity.Count);
            _playlists.Add(playlist);
            
            return playlist;
        }

        public LocalPlaylist CreatePlaylist(string label, IEnumerable<Interfaces.IVideoContent> firstItems)
        {
            var playlist = CreatePlaylist(label);
            AddPlaylistItem(playlist, firstItems);
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
            if (_playlistIdToEntity.TryGetValue(localPlaylist.Id, out var entity))
            {
                var result = _playlistRepository.Delete(entity);
            }

            return _playlists.Remove(localPlaylist);
        }

        public IEnumerable<IVideoContent> GetPlaylistItems(IPlaylist playlist)
        {
            var items = _playlistRepository.GetItems(playlist.Id);
            return Database.NicoVideoDb.Get(items.Select(x => x.ContentId));
        }


        public void AddPlaylistItem(IPlaylist playlist, IVideoContent item)
        {
            _playlistRepository.AddItem(playlist.Id, item.Id);
        }

        public void AddPlaylistItem(IPlaylist playlist, IEnumerable<IVideoContent> items)
        {
            _playlistRepository.AddItems(playlist.Id, items.Select(x => x.Id));
        }

        public bool RemovePlaylistItem(IPlaylist playlist, IVideoContent item)
        {
            return _playlistRepository.DeleteItem(playlist.Id, item.Id);
        }

        public int RemovePlaylistItems(IPlaylist playlist, IEnumerable<IVideoContent> items)
        {
            return _playlistRepository.DeleteItems(playlist.Id, items.Select(x => x.Id));
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
                _playlists.Remove(group);
            }
            , (p) => p != null && LocalPlaylists.Contains(p)
            ));




        
    }
}
