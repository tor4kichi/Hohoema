using NicoPlayerHohoema.Database.Local.LocalMylist;
using NicoPlayerHohoema.Models.Helpers;
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

namespace NicoPlayerHohoema.Models.LocalMylist
{
    public sealed class LocalMylistManager : IDisposable
    {
        public LocalMylistManager(
            NiconicoSession niconicoSession, 
            Services.NotificationService notificationService
            )
        {
            NiconicoSession = niconicoSession;
            NotificationService = notificationService;

            LocalMylistGroups = new ObservableCollection<LocalMylistGroup>(RestoreLocalMylistGroupsFromLocalDatabase());

            LocalMylistGroups.CollectionChangedAsObservable()
                .Subscribe(e =>
                {
                    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                    {
                        foreach (var newItem in e.NewItems.Cast<LocalMylistGroup>())
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
        }

        public ObservableCollection<LocalMylistGroup> LocalMylistGroups { get; }

        public NiconicoSession NiconicoSession { get; }
        public Services.NotificationService NotificationService { get; }

        Dictionary<string, IDisposable> LocalMylistPropertyChangedObserverMap = new Dictionary<string, IDisposable>();

        public void Dispose()
        {
            foreach (var disposer in LocalMylistPropertyChangedObserverMap.Values)
            {
                disposer.Dispose();
            }
        }



        private DelegateCommand<string> _AddCommand;
        public DelegateCommand<string> AddCommand => _AddCommand
            ?? (_AddCommand = new DelegateCommand<string>((label) =>
            {
                LocalMylistGroups.Add(new LocalMylistGroup(Guid.NewGuid().ToString(), label));
            }
            , (p) => !string.IsNullOrWhiteSpace(p)
            ));


        private DelegateCommand<LocalMylistGroup> _RemoveCommand;
        public DelegateCommand<LocalMylistGroup> RemoveCommand => _RemoveCommand
            ?? (_RemoveCommand = new DelegateCommand<LocalMylistGroup>((group) =>
            {
                LocalMylistGroups.Remove(group);
            }
            , (p) => p != null && LocalMylistGroups.Contains(p)
            ));


        static private List<LocalMylistGroup> RestoreLocalMylistGroupsFromLocalDatabase()
        {
            var groups = LocalMylistDb.GetLocalMylistGroups();
            return groups
                .Select(x => new LocalMylistGroup(x.Id, x.Label, x.Items))
                .ToList()
                ;
        }



        #region Migrate 0.15.x


        public const string PlaylistSaveFolderName = "Playlists";


        public static async Task<StorageFolder> GetPlaylistsSaveFolder()
        {
            return await ApplicationData.Current.LocalFolder.GetFolderAsync(PlaylistSaveFolderName);
        }

        public static async Task<List<LegacyLocalMylist>> LoadLegacyLocalMylistGroups()
        {
            var folder = await GetPlaylistsSaveFolder();
            if (folder == null) { return new List<LegacyLocalMylist>(); }

            var files = await folder.GetFilesAsync();

            // 読み込み
            List<LegacyLocalMylist> loadedItem = new List<LegacyLocalMylist>();
            foreach (var file in files)
            {
                var playlistFileAccessor = new FolderBasedFileAccessor<LegacyLocalMylist>(folder, file.Name);
                var playlist = await playlistFileAccessor.Load();

                if (playlist != null)
                {
                    loadedItem.Add(playlist);
                }
            }

            loadedItem.Sort((x, y) => x.SortIndex - y.SortIndex);

            return loadedItem;
        }

        #endregion


    }
}
