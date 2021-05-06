using Hohoema.Models.Domain;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading;
//using Hohoema.Models.Helpers;
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
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Collections;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using Hohoema.Models.UseCase.VideoCache.Events;
using Microsoft.Toolkit.Mvvm.Messaging;
using Uno.Extensions;

namespace Hohoema.Presentation.ViewModels.Pages.Hohoema.Video
{
    public class CacheManagementPageViewModel : HohoemaPageViewModelBase, INavigatedAwareAsync,
        IRecipient<VideoCacheStatusChangedMessage>
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

            Groups = new (new[] 
            {
                VideoCacheStatus.Downloading,
                VideoCacheStatus.Failed,
                VideoCacheStatus.DownloadPaused,
                VideoCacheStatus.Pending,
                VideoCacheStatus.Completed,
            }
            .Select(x => new CacheItemsGroup(x, new ObservableCollection<CacheVideoViewModel>()))
            );

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

       

        public ObservableCollection<CacheItemsGroup> Groups { get; }

        public class CacheItemsGroup
        {
            public CacheItemsGroup(VideoCacheStatus cacheStatus, ObservableCollection<CacheVideoViewModel> items)
            {
                CacheStatus = cacheStatus;
                Items = items;
            }
            public VideoCacheStatus CacheStatus { get;  }
            public ObservableCollection<CacheVideoViewModel> Items { get; }
        }


        private bool IsAssecsendingCacheStatus(VideoCacheStatus status)
        {
            return status is VideoCacheStatus.Pending;
        }

        private async ValueTask<CacheVideoViewModel> ItemVMFromVideoCacheItem(VideoCacheItem item)
        {
            var video = await NicoVideoProvider.GetNicoVideoInfo(item.VideoId);
            return  new CacheVideoViewModel(item, video) { CacheRequestTime = item.RequestedAt };
        }

        async Task<IEnumerable<CacheVideoViewModel>> GetCachedItemByStatus(VideoCacheStatus status)
        {
            var isAssecsnding = status is VideoCacheStatus.Pending;
            var reqItems = VideoCacheManager.GetCacheRequestItemsRange(0, int.MaxValue, status, !isAssecsnding);

            CacheVideoViewModel[] list = new CacheVideoViewModel[reqItems.Count];
            int index = 0;
            foreach (var item in reqItems)
            {
                list[index] = await ItemVMFromVideoCacheItem(item);

                index++;
            }

            return list;
        }



        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            // キャッシュ管理系のイベントに登録して詳細情報の掲示
            
            base.OnNavigatedTo(parameters);
        }


        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            foreach (var group in Groups)
            {
                var items = await GetCachedItemByStatus(group.CacheStatus);
                group.Items.Clear();
                group.Items.AddRange(items);
            }

            WeakReferenceMessenger.Default.Register<VideoCacheStatusChangedMessage>(this);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            WeakReferenceMessenger.Default.Unregister<VideoCacheStatusChangedMessage>(this);

            base.OnNavigatedFrom(parameters);
        }

        async void IRecipient<VideoCacheStatusChangedMessage>.Receive(VideoCacheStatusChangedMessage message)
        {
            var status = message.Value.CacheStatus;
            CacheVideoViewModel itemVM = null;
            foreach (var group in Groups)
            {
                itemVM = group.Items.FirstOrDefault(x => x.Id == message.Value.VideoId);
                
                if (itemVM != null) 
                {
                    group.Items.Remove(itemVM);
                    break; 
                }
            }

            if (status == null) 
            {
                itemVM?.Dispose();
                return; 
            }

            {
                itemVM ??= await ItemVMFromVideoCacheItem(message.Value.Item);

                var group = Groups.First(x => x.CacheStatus == status);
                if (group == null) { throw new InvalidOperationException(); }

                if (IsAssecsendingCacheStatus(status ?? throw new InvalidOperationException()))
                {
                    group.Items.Add(itemVM);
                }
                else
                {
                    group.Items.Insert(0, itemVM);
                }
            }
        }
    }


    public class CacheVideoViewModel : VideoItemViewModel, IDisposable,
        IRecipient<VideoCacheProgressChangedMessage>
    {
        public CacheVideoViewModel(
            VideoCacheItem videoCacheItem,
            IVideoContent data
            )
            : this(videoCacheItem, data.Id, data.Label, data.ThumbnailUrl, data.Length)
        {
        
        }

        private object recipient = new object();

        public CacheVideoViewModel(VideoCacheItem videoCacheItem, string rawVideoId, string title, string thumbnailUrl, TimeSpan videoLength) : base(rawVideoId, title, thumbnailUrl, videoLength)
        {
            WeakReferenceMessenger.Default.Register<VideoCacheStatusChangedMessage, string>(recipient, RawVideoId, (r, m) => RefreshCacheRequestInfomation(m.Value.CacheStatus, m.Value.Item));
            RefreshCacheRequestInfomation(videoCacheItem.Status, videoCacheItem);
        }

        

        public override void Dispose()
        {
            base.Dispose();

            WeakReferenceMessenger.Default.Unregister<VideoCacheStatusChangedMessage, string>(recipient, RawVideoId);
            WeakReferenceMessenger.Default.Unregister<VideoCacheProgressChangedMessage, string>(this, RawVideoId);
        }

        public DateTime CacheRequestTime { get; internal set; }


        private bool _HasCacheProgress;
        public bool HasCacheProgress
        {
            get { return _HasCacheProgress; }
            set { SetProperty(ref _HasCacheProgress, value); }
        }

        private double _DownloadProgress;
        public double DownloadProgress
        {
            get { return _DownloadProgress; }
            set { SetProperty(ref _DownloadProgress, value); }
        }

        private bool _IsProgressUnknown;
        public bool IsProgressUnknown
        {
            get { return _IsProgressUnknown; }
            set { SetProperty(ref _IsProgressUnknown, value); }
        }

        private VideoCacheDownloadOperationFailedReason _FailedReason;
        public VideoCacheDownloadOperationFailedReason FailedReason
        {
            get { return _FailedReason; }
            set { SetProperty(ref _FailedReason, value); }
        }

        private long? _FileSize;
        public long? FileSize
        {
            get { return _FileSize; }
            set { SetProperty(ref _FileSize, value); }
        }

        void RefreshCacheRequestInfomation(VideoCacheStatus? cacheStatus, VideoCacheItem cacheItem = null)
        {
            _scheduler.Schedule(() =>
            {
                DownloadProgress = cacheItem?.GetProgressNormalized() ?? 0;
                HasCacheProgress = cacheStatus is VideoCacheStatus.Downloading or VideoCacheStatus.DownloadPaused;
                IsProgressUnknown = HasCacheProgress && cacheItem.ProgressBytes is null or 0;
                FailedReason = cacheItem?.FailedReason ?? VideoCacheDownloadOperationFailedReason.None;
                FileSize = cacheItem?.TotalBytes;

                if (cacheStatus is VideoCacheStatus.Downloading)
                {
                    if (!WeakReferenceMessenger.Default.IsRegistered<VideoCacheProgressChangedMessage, string>(this, RawVideoId))
                    {
                        WeakReferenceMessenger.Default.Register<VideoCacheProgressChangedMessage, string>(this, RawVideoId);
                    }
                }
                else
                {
                    WeakReferenceMessenger.Default.Unregister<VideoCacheProgressChangedMessage, string>(this, RawVideoId);
                }
            });
        }

        void IRecipient<VideoCacheProgressChangedMessage>.Receive(VideoCacheProgressChangedMessage message)
        {
            _scheduler.Schedule(() =>
            {
                var cacheItem = message.Value;
                DownloadProgress = cacheItem.GetProgressNormalized();
                HasCacheProgress = true;
                IsProgressUnknown = cacheItem.ProgressBytes is null or 0;
            });
        }

    }

}
