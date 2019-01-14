using Microsoft.Toolkit.Uwp.Helpers;
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
            NiconicoSession niconicoSession
            )
        {
            NiconicoSession = niconicoSession;

            Mylists = new ObservableCollection<LocalMylistGroup>(RestoreLocalMylistGroupsFromLocalDatabase());

            Mylists.CollectionChangedAsObservable()
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

        public ObservableCollection<LocalMylistGroup> Mylists { get; }

        public NiconicoSession NiconicoSession { get; }

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
                Mylists.Add(new LocalMylistGroup(Guid.NewGuid().ToString(), label));
            }
            , (p) => !string.IsNullOrWhiteSpace(p)
            ));


        private DelegateCommand<LocalMylistGroup> _RemoveCommand;
        public DelegateCommand<LocalMylistGroup> RemoveCommand => _RemoveCommand
            ?? (_RemoveCommand = new DelegateCommand<LocalMylistGroup>((group) =>
            {
                Mylists.Remove(group);
            }
            , (p) => p != null && Mylists.Contains(p)
            ));


        static private List<LocalMylistGroup> RestoreLocalMylistGroupsFromLocalDatabase()
        {
            var groups = LocalMylistDb.GetLocalMylistGroups();
            return groups
                .Select(x => new LocalMylistGroup(x.Id, x.Label, new ObservableCollection<string>(x.Items)))
                .ToList()
                ;
        }



        #region Migrate 0.15.x

        public static async Task<int> RestoreLegacyLocalMylistGroups(LocalMylistManager manager)
        {
            var localStorage = new LocalObjectStorageHelper();
            var isMigrateLocalMylist = localStorage.Read("is_migrate_local_mylist_0_17_0", false);
            if (!isMigrateLocalMylist)
            {
                var items = await LoadLegacyLocalMylistGroups();
                foreach (var regacyItem in items)
                {
                    manager.Mylists.Add(new LocalMylistGroup(regacyItem.Id, regacyItem.Label, new ObservableCollection<string>(regacyItem.PlaylistItems.Select(x => x.ContentId))));
                }
                localStorage.Save("is_migrate_local_mylist_0_17_0", true);

                return items.Count;
            }
            else
            {
                return 0;
            }
        }


        public static bool IsMigrated_0_17_0
        {
            get
            {
                var localStorage = new LocalObjectStorageHelper();
                return localStorage.Read("is_migrate_local_mylist_0_17_0", false);
            }
        }

        public static void SkipMigrate_0_17_0()
        {
            var localStorage = new LocalObjectStorageHelper();
            localStorage.Save("is_migrate_local_mylist_0_17_0", true);
        }

        private const string PlaylistSaveFolderName = "Playlists";

        private static async Task<StorageFolder> GetPlaylistsSaveFolder()
        {
            return await ApplicationData.Current.LocalFolder.GetFolderAsync(PlaylistSaveFolderName);
        }

        private static async Task<List<LegacyLocalMylist>> LoadLegacyLocalMylistGroups()
        {
            try
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
            catch
            {
                return new List<LegacyLocalMylist>();
            }
        }

        #endregion


    }
}
