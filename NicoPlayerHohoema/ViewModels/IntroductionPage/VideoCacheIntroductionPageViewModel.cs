using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Cache;
using NicoPlayerHohoema.Services;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class VideoCacheIntroductionPageViewModel : BindableBase
    {
        public VideoCacheIntroductionPageViewModel(
            CacheSettings cacheSettings,
            CacheSaveFolder cacheSaveFolder,
            DialogService dialogService
            )
        {
            CacheSettings = cacheSettings;
            CacheSaveFolder = cacheSaveFolder;
            HohoemaDialogService = dialogService;

            var hasCacheFolder = Windows.Storage.AccessCache.StorageApplicationPermissions.
                    FutureAccessList.Entries.Count > 0;
            IsCompleteCacheSettings = new ReactiveProperty<bool>(CacheSettings.IsUserAcceptedCache && hasCacheFolder)
                .AddTo(_Disposables);

            CanChangeCacheSettings = new ReactiveProperty<bool>(!IsCompleteCacheSettings.Value)
                .AddTo(_Disposables);
            RequireEnablingCacheCommand = CanChangeCacheSettings.ToAsyncReactiveCommand()
                .AddTo(_Disposables);

            RequireEnablingCacheCommand.Subscribe(async () =>
            {
                bool isAcceptedCache = CacheSettings.IsUserAcceptedCache;
                if (!isAcceptedCache)
                {
                    isAcceptedCache = await HohoemaDialogService.ShowAcceptCacheUsaseDialogAsync();
                    if (isAcceptedCache)
                    {
                        CacheSettings.IsEnableCache = true;
                        CacheSettings.IsUserAcceptedCache = true;

                        (App.Current).Resources["IsCacheEnabled"] = true;
                    }
                }

                if (isAcceptedCache)
                {
                    if (await CacheSaveFolder.ChangeUserDataFolder())
                    {
                        IsCompleteCacheSettings.Value = true;
                    }
                }
            })
            .AddTo(_Disposables);

        }

        public CacheSettings CacheSettings { get; }
        public CacheSaveFolder CacheSaveFolder { get; }
        public DialogService HohoemaDialogService { get; }

        public AsyncReactiveCommand RequireEnablingCacheCommand { get; private set; }

        public ReactiveProperty<bool> CanChangeCacheSettings { get; private set; }
        public ReactiveProperty<bool> IsCompleteCacheSettings { get; private set; }

        CompositeDisposable _Disposables = new CompositeDisposable();

       
    }
}
