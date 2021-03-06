﻿using Hohoema.Models.Domain;
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
using Hohoema.Models.UseCase.Playlist;
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
using Hohoema.Models.Domain.Niconico;
using NiconicoToolkit.Video;

namespace Hohoema.Presentation.ViewModels.Pages.Hohoema.VideoCache
{
    public class CacheManagementPageViewModel : HohoemaPageViewModelBase, INavigatedAwareAsync,
        IRecipient<VideoCacheStatusChangedMessage>
    {
        public CacheManagementPageViewModel(
            IScheduler scheduler,
            NiconicoSession niconicoSession,
            ApplicationLayoutManager applicationLayoutManager,
            VideoCacheSettings cacheSettings,
            VideoCacheManager videoCacheManager,
            VideoCacheFolderManager videoCacheFolderManager,
            VideoCacheDownloadOperationManager videoCacheDownloadOperationManager,
            NicoVideoProvider nicoVideoProvider,
            PageManager pageManager,
            DialogService dialogService,
            NotificationService notificationService,
            SelectionModeToggleCommand selectionModeToggleCommand,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand
            )
        {
            _scheduler = scheduler;
            _niconicoSession = niconicoSession;
            ApplicationLayoutManager = applicationLayoutManager;
            VideoCacheSettings = cacheSettings;
            VideoCacheManager = videoCacheManager;
            _videoCacheFolderManager = videoCacheFolderManager;
            _videoCacheDownloadOperationManager = videoCacheDownloadOperationManager;
            NicoVideoProvider = nicoVideoProvider;
            HohoemaDialogService = dialogService;
            NotificationService = notificationService;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
            Groups = new (new[] 
            {
                VideoCacheStatus.Failed,
                VideoCacheStatus.Downloading,
                VideoCacheStatus.DownloadPaused,
                VideoCacheStatus.Pending,
                VideoCacheStatus.Completed,
            }
            .Select(x => new CacheItemsGroup(x, new ObservableCollection<CacheVideoViewModel>()))
            );

            IsLoggedInWithPremiumMember = _niconicoSession.ObserveProperty(x => x.IsPremiumAccount).ToReadOnlyReactivePropertySlim(_niconicoSession.IsPremiumAccount)
                .AddTo(_CompositeDisposable);

            CurrentlyCachedStorageSize = VideoCacheSettings.ObserveProperty(x => x.CachedStorageSize).ToReadOnlyReactivePropertySlim(VideoCacheSettings.CachedStorageSize)
                .AddTo(_CompositeDisposable);

            MaxCacheStorageSize = VideoCacheSettings.ObserveProperty(x => x.MaxVideoCacheStorageSize).ToReadOnlyReactivePropertySlim(VideoCacheSettings.MaxVideoCacheStorageSize)
                .AddTo(_CompositeDisposable);

            IsAllowDownload = new ReactivePropertySlim<bool>(_videoCacheDownloadOperationManager.IsAllowDownload, mode: ReactivePropertyMode.DistinctUntilChanged);
            IsAllowDownload.Subscribe(isAllowDownload =>
            {
                if (isAllowDownload)
                {
                    _videoCacheDownloadOperationManager.ResumeDownload();
                }
                else
                {
                    _videoCacheDownloadOperationManager.SuspendDownload();
                }
            })
                .AddTo(_CompositeDisposable);

            AvairableStorageSizeNormalized = new[]
            {
                CurrentlyCachedStorageSize,
                MaxCacheStorageSize.Select(x => x ?? 0),
            }
            .CombineLatest()
            .Select(xy => xy[1] == 0 ? 0.0 : ((double)xy[0] / xy[1]))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_CompositeDisposable);
        }

        private readonly IScheduler _scheduler;
        private readonly NiconicoSession _niconicoSession;
        private readonly VideoCacheFolderManager _videoCacheFolderManager;
        private readonly VideoCacheDownloadOperationManager _videoCacheDownloadOperationManager;

        public VideoCacheManager VideoCacheManager { get; }
        public VideoCacheSettings VideoCacheSettings { get; }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public NotificationService NotificationService { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
        public DelegateCommand OpenCurrentCacheFolderCommand { get; }
        public DialogService HohoemaDialogService { get; }

        public IReadOnlyReactiveProperty<bool> IsLoggedInWithPremiumMember { get; }

        public IReactiveProperty<bool> IsAllowDownload { get; }

        public IReadOnlyReactiveProperty<long> CurrentlyCachedStorageSize { get; }
        public IReadOnlyReactiveProperty<long?> MaxCacheStorageSize { get; }
        public IReadOnlyReactiveProperty<double> AvairableStorageSizeNormalized { get; }

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

        private bool _HasNoItems;
        public bool HasNoItems
        {
            get => _HasNoItems;
            set => SetProperty(ref _HasNoItems, value);
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
            var video = await NicoVideoProvider.GetCachedVideoInfoAsync(item.VideoId);
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
            HasNoItems = false;
            bool anyItems = false;
            foreach (var group in Groups)
            {
                var items = await GetCachedItemByStatus(group.CacheStatus);
                group.Items.Clear();
                group.Items.AddRange(items);

                anyItems |= group.Items.Any();
            }

            HasNoItems = !anyItems;

            WeakReferenceMessenger.Default.Register<VideoCacheStatusChangedMessage>(this);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            WeakReferenceMessenger.Default.Unregister<VideoCacheStatusChangedMessage>(this);

            base.OnNavigatedFrom(parameters);
        }

        void IRecipient<VideoCacheStatusChangedMessage>.Receive(VideoCacheStatusChangedMessage message)
        {
            _scheduler.Schedule((Action)(async () =>
            {
                var status = message.Value.CacheStatus;
                CacheVideoViewModel itemVM = null;
                foreach (var group in Groups)
                {
                    itemVM = group.Items.FirstOrDefault((Func<CacheVideoViewModel, bool>)(x => x.VideoId == message.Value.VideoId));

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
            }));
        }
    }


    public class CacheVideoViewModel : VideoItemViewModel, IDisposable,
        IRecipient<VideoCacheProgressChangedMessage>
    {
        public CacheVideoViewModel(
            VideoCacheItem videoCacheItem,
            IVideoContent data
            )
            : this(videoCacheItem, data.VideoId, data.Title, data.ThumbnailUrl, data.Length, data.PostedAt)
        {
        
        }

        private object recipient = new object();

        public CacheVideoViewModel(VideoCacheItem videoCacheItem, VideoId rawVideoId, string title, string thumbnailUrl, TimeSpan videoLength, DateTime postedAt)
            : base(rawVideoId, title, thumbnailUrl, videoLength, postedAt)
        {
            WeakReferenceMessenger.Default.Register<VideoCacheStatusChangedMessage, VideoId>(recipient, VideoId, (r, m) => RefreshCacheRequestInfomation(m.Value.CacheStatus, m.Value.Item));
            RefreshCacheRequestInfomation(videoCacheItem.Status, videoCacheItem);
        }

        

        public override void Dispose()
        {
            base.Dispose();

            WeakReferenceMessenger.Default.Unregister<VideoCacheStatusChangedMessage, VideoId>(recipient, VideoId);
            WeakReferenceMessenger.Default.Unregister<VideoCacheProgressChangedMessage, VideoId>(this, VideoId);
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
            _scheduler.Schedule((Action)(() =>
            {
                DownloadProgress = cacheItem?.GetProgressNormalized() ?? 0;
                HasCacheProgress = cacheStatus is VideoCacheStatus.Downloading or VideoCacheStatus.DownloadPaused;
                IsProgressUnknown = HasCacheProgress && cacheItem.ProgressBytes is null or 0;
                FailedReason = cacheItem?.FailedReason ?? VideoCacheDownloadOperationFailedReason.None;
                FileSize = cacheItem?.TotalBytes;

                if (cacheStatus is VideoCacheStatus.Downloading)
                {
                    if (!WeakReferenceMessenger.Default.IsRegistered<VideoCacheProgressChangedMessage, VideoId>(this, (string)base.VideoId))
                    {
                        WeakReferenceMessenger.Default.Register((IRecipient<VideoCacheProgressChangedMessage>)this, (string)base.VideoId);
                    }
                }
                else
                {
                    WeakReferenceMessenger.Default.Unregister<VideoCacheProgressChangedMessage, VideoId>(this, (string)base.VideoId);
                }
            }));
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
