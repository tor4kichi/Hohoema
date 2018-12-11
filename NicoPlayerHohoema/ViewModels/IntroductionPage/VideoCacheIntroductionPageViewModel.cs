using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;
using Prism.Commands;
using Prism.Windows.Mvvm;
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
    public sealed class VideoCacheIntroductionPageViewModel : ViewModelBase
    {
        public AsyncReactiveCommand RequireEnablingCacheCommand { get; private set; }

        public ReactiveProperty<bool> CanChangeCacheSettings { get; private set; }
        public ReactiveProperty<bool> IsCompleteCacheSettings { get; private set; }
        private DialogService _HohoemaDialogService;
        private HohoemaApp _HohoemaApp;

        CompositeDisposable _Disposables = new CompositeDisposable();

        public VideoCacheIntroductionPageViewModel(DialogService dialogService, HohoemaApp hohoemaApp)
        {
            _HohoemaDialogService = dialogService;
            _HohoemaApp = hohoemaApp;

            var hasCacheFolder = Windows.Storage.AccessCache.StorageApplicationPermissions.
                    FutureAccessList.Entries.Count > 0;
            IsCompleteCacheSettings = new ReactiveProperty<bool>(_HohoemaApp.UserSettings.CacheSettings.IsUserAcceptedCache && hasCacheFolder)
                .AddTo(_Disposables);

            CanChangeCacheSettings = new ReactiveProperty<bool>(!IsCompleteCacheSettings.Value)
                .AddTo(_Disposables);
            RequireEnablingCacheCommand = CanChangeCacheSettings.ToAsyncReactiveCommand()
                .AddTo(_Disposables);

            RequireEnablingCacheCommand.Subscribe(async () =>
            {
                bool isAcceptedCache = _HohoemaApp.UserSettings.CacheSettings.IsUserAcceptedCache;
                if (!isAcceptedCache)
                {
                    isAcceptedCache = await _HohoemaDialogService.ShowAcceptCacheUsaseDialogAsync();
                    if (isAcceptedCache)
                    {
                        _HohoemaApp.UserSettings.CacheSettings.IsEnableCache = true;
                        _HohoemaApp.UserSettings.CacheSettings.IsUserAcceptedCache = true;

                        (App.Current).Resources["IsCacheEnabled"] = true;
                    }
                }

                if (isAcceptedCache)
                {
                    if (await _HohoemaApp.ChangeUserDataFolder())
                    {
                        IsCompleteCacheSettings.Value = true;
                    }
                }
            })
            .AddTo(_Disposables);

        }
        
    }
}
