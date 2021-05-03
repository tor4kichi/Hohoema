using Hohoema.Models.Domain;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading;
using Hohoema.Models.Helpers;
using Prism.Commands;
using Windows.System;
using Prism.Navigation;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Models.UseCase;
using I18NPortable;
using System.Runtime.CompilerServices;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Models.Domain.Notification;
using Hohoema.Models.Domain.VideoCache;
using Microsoft.Extensions.ObjectPool;
using Hohoema.Models.UseCase.VideoCache;

namespace Hohoema.Presentation.ViewModels.Pages.Hohoema.Video
{
    public class CacheManagementPageViewModel : HohoemaListingPageViewModelBase<CacheVideoViewModel>, INavigatedAwareAsync
	{
        public CacheManagementPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            VideoCacheSettings cacheSettings,
            VideoCacheManager videoCacheManager,
            VideoCacheFolderManager videoCacheFolderManager,
            NicoVideoProvider nicoVideoProvider,
            PageManager pageManager,
            DialogService dialogService,
            NotificationService notificationService,
            HohoemaPlaylist hohoemaPlaylist,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            CacheSettings = cacheSettings;
            VideoCacheManager = videoCacheManager;
            _videoCacheFolderManager = videoCacheFolderManager;
            NicoVideoProvider = nicoVideoProvider;
            HohoemaDialogService = dialogService;
            NotificationService = notificationService;
            HohoemaPlaylist = hohoemaPlaylist;
            SelectionModeToggleCommand = selectionModeToggleCommand;

            OpenCurrentCacheFolderCommand = new DelegateCommand(async () =>
            {
                var folder = VideoCacheManager.VideoCacheFolder;
                if (folder != null)
                {
                    await Launcher.LaunchFolderAsync(folder);
                }
            });


        }
        private readonly VideoCacheFolderManager _videoCacheFolderManager;


        public VideoCacheManager VideoCacheManager { get; }
        public VideoCacheSettings CacheSettings { get; }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public NotificationService NotificationService { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public DelegateCommand OpenCurrentCacheFolderCommand { get; }
        public DialogService HohoemaDialogService { get; }

       
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

        private DelegateCommand _ChangeCacheVideoFolderCommand;
        public DelegateCommand ChangeCacheVideoFolderCommand => 
            _ChangeCacheVideoFolderCommand ??= new DelegateCommand(async () =>
            {
                await _videoCacheFolderManager.ChangeVideoCacheFolder();
            });





        #region Implement HohoemaVideListViewModelBase

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
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
                var folder = VideoCacheManager.VideoCacheFolder;
                await folder.GetBasicPropertiesAsync();
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
            NicoVideo data
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

        protected override async IAsyncEnumerable<CacheVideoViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var reqItems = VideoCacheManager.GetCacheRequestItemsRange(head, count);
            var items = await NicoVideoProvider.GetVideoInfoManyAsync(reqItems.Select(x => x.VideoId), isLatestRequired: false).ToListAsync(ct);
            foreach (var item in reqItems)
            {
                var video = items.FirstOrDefault(x => x.VideoId == item.VideoId);
                var vm = video is not null ? new CacheVideoViewModel(video) : new CacheVideoViewModel(item.VideoId);
                vm.CacheRequestTime = item.RequestedAt;

                if (video is null)
                {
                    await vm.InitializeAsync(ct).ConfigureAwait(false);
                }

                yield return vm;

                ct.ThrowIfCancellationRequested();
            }
        }

        protected override ValueTask<int> ResetSourceImpl()
        {
            return new ValueTask<int>(VideoCacheManager.GetCacheRequestCount());
        }
    }

}
