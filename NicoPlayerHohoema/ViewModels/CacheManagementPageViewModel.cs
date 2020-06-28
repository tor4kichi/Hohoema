﻿using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading;
using NicoPlayerHohoema.Models.Helpers;
using Prism.Commands;
using NicoPlayerHohoema.Services;
using Windows.System;
using NicoPlayerHohoema.Models.Cache;
using NicoPlayerHohoema.Models.Provider;
using Unity;
using Prism.Navigation;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.UseCase.Playlist;
using NicoPlayerHohoema.UseCase;
using I18NPortable;
using System.Runtime.CompilerServices;

namespace NicoPlayerHohoema.ViewModels
{
    public class CacheManagementPageViewModel : HohoemaListingPageViewModelBase<CacheVideoViewModel>, INavigatedAwareAsync
	{
        public CacheManagementPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            CacheSettings cacheSettings,
            VideoCacheManager videoCacheManager,
            CacheSaveFolder cacheSaveFolder,
            NicoVideoProvider nicoVideoProvider,
            PageManager pageManager,
            DialogService dialogService,
            NotificationService notificationService,
            HohoemaPlaylist hohoemaPlaylist
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            CacheSettings = cacheSettings;
            VideoCacheManager = videoCacheManager;
            CacheSaveFolder = cacheSaveFolder;
            NicoVideoProvider = nicoVideoProvider;
            HohoemaDialogService = dialogService;
            NotificationService = notificationService;
            HohoemaPlaylist = hohoemaPlaylist;
            IsRequireUpdateCacheSaveFolder = new ReactiveProperty<bool>(false);

            IsCacheUserAccepted = CacheSettings.ObserveProperty(x => x.IsUserAcceptedCache)
                .ToReadOnlyReactiveProperty();

            RequireEnablingCacheCommand = new DelegateCommand(async () =>
            {
                var result = await HohoemaDialogService.ShowAcceptCacheUsaseDialogAsync();
                if (result)
                {
                    CacheSettings.IsEnableCache = true;
                    CacheSettings.IsUserAcceptedCache = true;
                    (App.Current).Resources["IsCacheEnabled"] = true;

                    await RefreshCacheSaveFolderStatus();

                    NotificationService.ShowInAppNotification(
                        InAppNotificationPayload.CreateReadOnlyNotification("ChoiceCacheSavingFolder".Translate(),
                        showDuration: TimeSpan.FromSeconds(30)
                        ));

                    if (await CacheSaveFolder.ChangeUserDataFolder())
                    {
                        await RefreshCacheSaveFolderStatus();

                        await VideoCacheManager.CacheFolderChanged();

                        await ResetList();

                        NotificationService.ShowInAppNotification(
                            InAppNotificationPayload.CreateReadOnlyNotification("ReadyForVideoCache".Translate())
                            );
                    }
                }
            });

            ReadCacheAcceptTextCommand = new DelegateCommand(async () =>
            {
                var result = await HohoemaDialogService.ShowAcceptCacheUsaseDialogAsync(showWithoutConfirmButton: true);
            });



            CacheFolderStateDescription = new ReactiveProperty<string>("");
            CacheSaveFolderPath = new ReactiveProperty<string>("");

            OpenCurrentCacheFolderCommand = new DelegateCommand(async () =>
            {
                await RefreshCacheSaveFolderStatus();

                var folder = await CacheSaveFolder.GetVideoCacheFolder();
                if (folder != null)
                {
                    await Launcher.LaunchFolderAsync(folder);
                }
            });


            ChangeCacheFolderCommand = new DelegateCommand(async () =>
            {
                var prevPath = CacheSaveFolderPath.Value;

                if (await CacheSaveFolder.ChangeUserDataFolder())
                {
                    NotificationService.ShowInAppNotification(
                        InAppNotificationPayload.CreateReadOnlyNotification("CacheSaveFolderChangeToX".Translate(CacheSaveFolderPath.Value))
                        );

                    await RefreshCacheSaveFolderStatus();

                    await VideoCacheManager.CacheFolderChanged();

                    await ResetList();
                }
            });
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public CacheSettings CacheSettings { get; }
        public VideoCacheManager VideoCacheManager { get; }
        public CacheSaveFolder CacheSaveFolder { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public NotificationService NotificationService { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public DialogService HohoemaDialogService { get; }



        public ReadOnlyReactiveProperty<bool> IsCacheUserAccepted { get; private set; }
        public ReactiveProperty<bool> IsRequireUpdateCacheSaveFolder { get; private set; }

        public ReactiveProperty<string> CacheSaveFolderPath { get; private set; }
        public DelegateCommand OpenCurrentCacheFolderCommand { get; private set; }
        public ReactiveProperty<string> CacheFolderStateDescription { get; private set; }


        public DelegateCommand ChangeCacheFolderCommand { get; private set; }
        public DelegateCommand CheckExistCacheFolderCommand { get; private set; }
        public DelegateCommand RequireEnablingCacheCommand { get; private set; }
        public DelegateCommand ReadCacheAcceptTextCommand { get; private set; }

        private DelegateCommand _ResumeCacheCommand;
        public DelegateCommand ResumeCacheCommand
        {
            get
            {
                return _ResumeCacheCommand
                    ?? (_ResumeCacheCommand = new DelegateCommand(() =>
                    {
                        // TODO: バックグラウンドダウンロードの強制更新？
                        //await _MediaManager.StartBackgroundDownload();
                    }));
            }
        }




        #region Implement HohoemaVideListViewModelBase

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            await RefreshCacheSaveFolderStatus();

            if (IsRequireUpdateCacheSaveFolder.Value)
            {
                NotificationService.ShowInAppNotification(
                    InAppNotificationPayload.CreateReadOnlyNotification( "ChoiceCacheSavingFolder".Translate(),
                    showDuration: TimeSpan.FromSeconds(30)
                    ));

                if (await CacheSaveFolder.ChangeUserDataFolder())
                {
                    await Task.Delay(1000);

                    await RefreshCacheSaveFolderStatus();
                    await ResetList();

                    NotificationService.ShowInAppNotification(
                        InAppNotificationPayload.CreateReadOnlyNotification("ReadyForVideoCache".Translate())
                        );
                }
            }

            await base.OnNavigatedToAsync(parameters);
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            Windows.UI.Xaml.Window.Current.Activated += Current_Activated;
            base.OnNavigatedTo(parameters);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            Windows.UI.Xaml.Window.Current.Activated -= Current_Activated;
            base.OnNavigatedFrom(parameters);
        }

        // ウィンドウがアクティブになったタイミングで
        // キャッシュフォルダ―が格納されたストレージをホットスタンバイ状態にしたい
        // （コールドスタンバイ状態だと再生開始までのラグが大きい）
        private async void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.CodeActivated)
            {
                var folder = await CacheSaveFolder.GetVideoCacheFolder();
            }
        }




        protected override IIncrementalSource<CacheVideoViewModel> GenerateIncrementalSource()
		{
			return new CacheVideoInfoLoadingSource(VideoCacheManager, NicoVideoProvider);
		}

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			return mode == NavigationMode.New;
		}

		protected override void PostResetList()
		{
			
		}




        #endregion

        private async Task RefreshCacheSaveFolderStatus()
        {
            var cacheFolderAccessState = await CacheSaveFolder.GetVideoCacheFolderState();

            CacheSaveFolderPath.Value = "";
            switch (cacheFolderAccessState)
            {
                case CacheFolderAccessState.NotAccepted:
                    CacheFolderStateDescription.Value = "CacheFolderAccessState.NotAccepted_Desc".Translate();
                    break;
                case CacheFolderAccessState.NotEnabled:
                    CacheFolderStateDescription.Value = "CacheFolderAccessState.NotEnabled_Desc".Translate();
                    break;
                case CacheFolderAccessState.NotSelected:
                    CacheFolderStateDescription.Value = "CacheFolderAccessState.NotSelected_Desc".Translate();
                    break;
                case CacheFolderAccessState.SelectedButNotExist:
                    CacheFolderStateDescription.Value = "CacheFolderAccessState.SelectedButNotExist_Desc".Translate();
                    CacheSaveFolderPath.Value = "?????";
                    break;
                case CacheFolderAccessState.Exist:
                    CacheFolderStateDescription.Value = "ReadyForVideoCache".Translate();
                    break;
                default:
                    break;
            }

            var folder = await CacheSaveFolder.GetVideoCacheFolder();
            if (folder != null)
            {
                CacheSaveFolderPath.Value = $"{folder.Path}";
            }


            IsRequireUpdateCacheSaveFolder.Value = 
                cacheFolderAccessState == CacheFolderAccessState.SelectedButNotExist
                || cacheFolderAccessState == CacheFolderAccessState.NotSelected
                ;
        }
    }


    public class CacheVideoViewModel : VideoInfoControlViewModel
	{

        public CacheVideoViewModel(
            string rawVideoId
            )
            : base(rawVideoId)
        {

        }

        public CacheVideoViewModel(
            Database.NicoVideo data
            )
            : base(data)
        {

        }

        public DateTime CacheRequestTime { get; internal set; }
    }


	public class CacheVideoInfoLoadingSource : HohoemaIncrementalSourceBase<CacheVideoViewModel>
	{

        public CacheVideoInfoLoadingSource(VideoCacheManager cacheManager, NicoVideoProvider nicoVideoProvider)
            : base()
        {
            VideoCacheManager = cacheManager;
            NicoVideoProvider = nicoVideoProvider;
        }

        public VideoCacheManager VideoCacheManager { get; }
        public NicoVideoProvider NicoVideoProvider { get; }

        public override uint OneTimeLoadCount => (uint)10;

        protected override async IAsyncEnumerable<CacheVideoViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach(var item in  VideoCacheManager.GetCacheRequests(head, count))
            {
                var vm = new CacheVideoViewModel(item.VideoId);
                vm.CacheRequestTime = item.RequestAt;
                await vm.InitializeAsync(cancellationToken);
                yield return vm;
            }
        }

        protected override Task<int> ResetSourceImpl()
        {
            return Task.FromResult(VideoCacheManager.GetCacheRequestCount());
        }
    }

}
