using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using Hohoema.Services.Navigations;
using Hohoema.Services.Subscriptions;
using Hohoema.Services.Navigations;
using Hohoema.Services;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.VideoListPage;
using I18NPortable;
using Microsoft.Extensions.Logging;
using NiconicoToolkit.Video;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Windows.UI.Xaml.Controls;
using ZLogger;

namespace Hohoema.ViewModels.Pages.Hohoema.Subscription
{
    public partial class SubscriptionManagementPageViewModel : HohoemaPageViewModelBase, IRecipient<SettingsRestoredMessage>, IDisposable
    {
        public ObservableCollection<SubscriptionViewModel> Subscriptions { get; }

        private readonly SubscriptionManager _subscriptionManager;
        private readonly SubscriptionUpdateManager _subscriptionUpdateManager;
        private readonly DialogService _dialogService;
        private readonly PageManager _pageManager;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly QueuePlaylist _queuePlaylist;
        private readonly VideoPlayWithQueueCommand _VideoPlayWithQueueCommand;
        private readonly IScheduler _scheduler;
        private readonly IMessenger _messenger;

        public IReadOnlyReactiveProperty<bool> IsAutoUpdateRunning { get; }
        public IReactiveProperty<TimeSpan> AutoUpdateFrequency { get; }
        public IReactiveProperty<bool> IsAutoUpdateEnabled { get; }

        void IRecipient<SettingsRestoredMessage>.Receive(SettingsRestoredMessage message)
        {
            
        }

        public SubscriptionManagementPageViewModel(
            ILoggerFactory loggerFactory,
            IScheduler scheduler, 
            IMessenger messenger,
            SubscriptionManager subscriptionManager,
            SubscriptionUpdateManager subscriptionUpdateManager,
            DialogService dialogService,
            PageManager pageManager,
            NicoVideoProvider nicoVideoProvider,
            QueuePlaylist queuePlaylist,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand
            )
        {
            _logger = loggerFactory.CreateLogger<SubscriptionManagementPageViewModel>();
            _scheduler = scheduler;
            _messenger = messenger;
            _subscriptionManager = subscriptionManager;
            _subscriptionUpdateManager = subscriptionUpdateManager;
            _dialogService = dialogService;
            _pageManager = pageManager;
            _nicoVideoProvider = nicoVideoProvider;
            _queuePlaylist = queuePlaylist;
            _VideoPlayWithQueueCommand = videoPlayWithQueueCommand;

            _messenger.Register<SettingsRestoredMessage>(this);

            Subscriptions = new ObservableCollection<SubscriptionViewModel>();
            Subscriptions.CollectionChangedAsObservable()
               .Throttle(TimeSpan.FromSeconds(0.25))
               .Subscribe(_ =>
               {
                   foreach (var (index, vm) in Subscriptions.Select((x, i) => (i, x)))
                   {
                       var subscEntity = vm._source;
                       subscEntity.SortIndex = index + 1; // 新規追加時に既存アイテムを後ろにずらして表示したいため+1
                       _subscriptionManager.UpdateSubscription(subscEntity);
                   }
               })
               .AddTo(_CompositeDisposable);

            IsAutoUpdateRunning = _subscriptionUpdateManager.ObserveProperty(x => x.IsRunning)
                .ToReadOnlyReactiveProperty(false)
                .AddTo(_CompositeDisposable);
            AutoUpdateFrequency = _subscriptionUpdateManager.ToReactivePropertyAsSynchronized(x => x.UpdateFrequency)
                .AddTo(_CompositeDisposable);
            IsAutoUpdateEnabled = _subscriptionUpdateManager.ToReactivePropertyAsSynchronized(x => x.IsAutoUpdateEnabled)
                .AddTo(_CompositeDisposable);
        }

        

        public override void Dispose()
        {
            foreach (var subscription in Subscriptions)
            {
                subscription.Dispose();
            }

            base.Dispose();
        }

        public override void OnNavigatingTo(INavigationParameters parameters)
        {
            Subscriptions.Clear();
            foreach (var subscInfo in _subscriptionManager.GetAllSubscriptionSourceEntities().OrderBy(x => x.SortIndex))
            {
                var vm = new SubscriptionViewModel(_logger, _messenger, _queuePlaylist, subscInfo, this, _subscriptionManager, _pageManager, _dialogService, _VideoPlayWithQueueCommand);
                var latestVideo = _subscriptionManager.GetSubscFeedVideos(subscInfo, 0, 1).FirstOrDefault();
                if (latestVideo != null)
                {
                    var items = _nicoVideoProvider.GetCachedVideoInfoItems(new[] { (VideoId)latestVideo.VideoId });
                    vm.UpdateFeedResult(items, subscInfo.LastUpdateAt);
                }
                Subscriptions.Add(vm);
            }

            _messenger.Register<NewSubscMessage>(this, (r, m) => 
            {
                _scheduler.Schedule(() =>
                {
                    var vm = new SubscriptionViewModel(_logger, _messenger, _queuePlaylist, m.Value, this, _subscriptionManager, _pageManager, _dialogService, _VideoPlayWithQueueCommand);
                    Subscriptions.Insert(0, vm);
                });
            });

            _messenger.Register<DeleteSubscMessage>(this, (r, m) =>
            {
                var entityId = m.Value;
                var target = Subscriptions.FirstOrDefault(x => x._source.Id == entityId);
                if (target == null) { return; }

                _scheduler.Schedule(() =>
                {
                    target.Dispose();
                    Subscriptions.Remove(target);
                });
            });

            _messenger.Register<UpdateSubscMessage>(this, (r, m) =>
            {
                var entity = m.Value;
                var vm = Subscriptions.FirstOrDefault(x => x.SourceType == entity.SourceType && x.SourceParameter == entity.SourceParameter);
                if (vm != null)
                {
                    //_scheduler.Schedule(() =>
                    //{
                    //    vm.UpdateFeedResult(e.Videos, DateTime.Now);
                    //});
                }
            });

            base.OnNavigatingTo(parameters);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            _messenger.Unregister<NewSubscMessage>(this);
            _messenger.Unregister<DeleteSubscMessage>(this);
            _messenger.Unregister<UpdateSubscMessage>(this);

            base.OnNavigatedFrom(parameters);
        }


        NicoVideo ToVideoContent(FeedResultVideoItem item)
        {
            return _nicoVideoProvider.GetCachedVideoInfo(item.VideoId);
        }


        #region PlayAllUnwatched

        private RelayCommand _PlayAllUnwatchedCommand;
        public RelayCommand PlayAllUnwatchedCommand =>
            _PlayAllUnwatchedCommand ?? (_PlayAllUnwatchedCommand = new RelayCommand(ExecutePlayAllUnwatchedCommand, CanExecutePlayAllUnwatchedCommand));

        void ExecutePlayAllUnwatchedCommand()
        {

        }

        bool CanExecutePlayAllUnwatchedCommand()
        {
            return true;
        }

        #endregion

        #region AddSubscriptionSource

        private RelayCommand _AddSubscriptionSourceCommand;
        public RelayCommand AddSubscriptionSourceCommand =>
            _AddSubscriptionSourceCommand ?? (_AddSubscriptionSourceCommand = new RelayCommand(ExecuteAddSubscriptionSourceCommand, CanExecuteAddSubscriptionSourceCommand));

        void ExecuteAddSubscriptionSourceCommand()
        {

        }

        bool CanExecuteAddSubscriptionSourceCommand()
        {
            return true;
        }

        #endregion


        CancellationTokenSource _cancellationTokenSource;

        private RelayCommand _AllUpdateCommand;
        public RelayCommand AllUpdateCommand =>
            _AllUpdateCommand ?? (_AllUpdateCommand = new RelayCommand(ExecuteAllUpdateCommand));

        void ExecuteAllUpdateCommand()
        {
            _scheduler.Schedule(async () =>
            {
                using (_cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                {
                    try
                    {
                        await _subscriptionUpdateManager.UpdateIfOverExpirationAsync(_cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        
                    }
                }
            });

            //Analytics.TrackEvent("Subscription_Update");
        }


        private RelayCommand _CancelUpdateCommand;
        private ILogger<SubscriptionManagementPageViewModel> _logger;

        public RelayCommand CancelUpdateCommand =>
            _CancelUpdateCommand ?? (_CancelUpdateCommand = new RelayCommand(ExecuteCancelUpdateCommand));

        void ExecuteCancelUpdateCommand()
        {
            _cancellationTokenSource?.Cancel();
        }



        public void OpenSourceVideoListPage(object sender, ItemClickEventArgs args)
        {
            var source = (args.ClickedItem as SubscriptionViewModel)._source;
            (HohoemaPageType pageType, string param) pageInfo = source.SourceType switch
            {
                SubscriptionSourceType.Mylist => (HohoemaPageType.Mylist, $"id={source.SourceParameter}"),
                SubscriptionSourceType.User => (HohoemaPageType.UserVideo, $"id={source.SourceParameter}"),
                SubscriptionSourceType.Channel => (HohoemaPageType.ChannelVideo, $"id={source.SourceParameter}"),
                SubscriptionSourceType.Series => (HohoemaPageType.Series, $"id={source.SourceParameter}"),
                SubscriptionSourceType.SearchWithKeyword => (HohoemaPageType.SearchResultKeyword, $"keyword={Uri.EscapeDataString(source.SourceParameter)}"),
                SubscriptionSourceType.SearchWithTag => (HohoemaPageType.SearchResultTag, $"keyword={Uri.EscapeDataString(source.SourceParameter)}"),
                _ => throw new NotImplementedException(),
            };

            _pageManager.OpenPage(pageInfo.pageType, pageInfo.param);

        }



        [RelayCommand]
        void OpenSubscVideoListPage()
        {
            _pageManager.OpenPage(HohoemaPageType.SubscVideoList);
        }
    }



    public partial class SubscriptionViewModel : ObservableObject, IDisposable
    {
        public SubscriptionViewModel(
            ILogger logger,
            IMessenger messenger,
            QueuePlaylist queuePlaylist,
            SubscriptionSourceEntity source,
            SubscriptionManagementPageViewModel pageViewModel,
            SubscriptionManager subscriptionManager,
            PageManager pageManager,
            DialogService dialogService,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand
            )
        {
            _logger = logger;
            _messenger = messenger;
            _queuePlaylist = queuePlaylist;
            _source = source;
            _pageViewModel = pageViewModel;
            _subscriptionManager = subscriptionManager;
            _pageManager = pageManager;
            _dialogService = dialogService;
            PlayVideoItemCommand = videoPlayWithQueueCommand;
            IsEnabled = new ReactiveProperty<bool>(_source.IsEnabled)
                .AddTo(_disposables);
            IsEnabled.Subscribe(isEnabled => 
            {
                _source.IsEnabled = isEnabled;
                _subscriptionManager.UpdateSubscription(_source);
            })
                .AddTo(_disposables);
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ILogger _logger;
        private readonly IMessenger _messenger;
        private readonly QueuePlaylist _queuePlaylist;
        internal readonly SubscriptionSourceEntity _source;
        private readonly SubscriptionManagementPageViewModel _pageViewModel;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly PageManager _pageManager;
        private readonly DialogService _dialogService;
        public VideoPlayWithQueueCommand PlayVideoItemCommand { get; }

        public string SourceParameter => _source.SourceParameter;
        public SubscriptionSourceType SourceType => _source.SourceType;
        public string Label => _source.Label;

        public ReactiveProperty<bool> IsEnabled { get; }

        //public ObservableCollection<VideoListItemControlViewModel> Videos { get; } = new ObservableCollection<VideoListItemControlViewModel>();        

        [ObservableProperty]
        private VideoListItemControlViewModel _sampleVideo;

        private DateTime _lastUpdatedAt;
        public DateTime LastUpdatedAt
        {
            get { return _lastUpdatedAt; }
            set { SetProperty(ref _lastUpdatedAt, value); }
        }

        private bool _nowUpdating;
        public bool NowUpdating
        {
            get { return _nowUpdating; }
            set { SetProperty(ref _nowUpdating, value); }
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _sampleVideo?.Dispose();
        }

        internal void UpdateFeedResult(IList<NicoVideo> result, DateTime updatedAt)
        {
            _sampleVideo?.Dispose();
            _sampleVideo = null;
            // Count == 0 の時にClearすると ArgumentOutOfRangeException がスローされてしまう

            if (result.FirstOrDefault() is not null and var video)
            {
                _sampleVideo = new VideoListItemControlViewModel(video);
            }

            LastUpdatedAt = updatedAt;
        }

        private RelayCommand _UpdateCommand;
        public RelayCommand UpdateCommand =>
            _UpdateCommand ?? (_UpdateCommand = new RelayCommand(ExecuteUpdateCommand));

        internal async void ExecuteUpdateCommand()
        {
            try
            {
                NowUpdating = true;
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    await _subscriptionManager.RefreshFeedUpdateResultAsync(_source, cts.Token);
                }
            }
            catch (Exception e)
            {
                _logger.ZLogErrorWithPayload(e, _source, "Subscription update failed");
            }
            finally
            {
                NowUpdating = false;
            }
        }

        private RelayCommand _OpenSourceVideoListPageCommand;
        public RelayCommand OpenSourceVideoListPageCommand =>
            _OpenSourceVideoListPageCommand ?? (_OpenSourceVideoListPageCommand = new RelayCommand(ExecuteOpenSourceVideoListPageCommand));

        void ExecuteOpenSourceVideoListPageCommand()
        {
            (HohoemaPageType pageType, string param) pageInfo = _source.SourceType switch
            {
                SubscriptionSourceType.Mylist => (HohoemaPageType.Mylist, $"id={_source.SourceParameter}"),
                SubscriptionSourceType.User => (HohoemaPageType.UserVideo, $"id={_source.SourceParameter}"),
                SubscriptionSourceType.Channel => (HohoemaPageType.ChannelVideo, $"id={_source.SourceParameter}"),
                SubscriptionSourceType.Series => (HohoemaPageType.Series, $"id={_source.SourceParameter}"),
                SubscriptionSourceType.SearchWithKeyword => (HohoemaPageType.SearchResultKeyword, $"keyword={Uri.EscapeDataString(_source.SourceParameter)}"),
                SubscriptionSourceType.SearchWithTag => (HohoemaPageType.SearchResultTag, $"keyword={Uri.EscapeDataString(_source.SourceParameter)}"),
                _ => throw new NotImplementedException(),
            };

            _pageManager.OpenPage(pageInfo.pageType, pageInfo.param);
        }



        private RelayCommand _DeleteSubscriptionCommand;
        public RelayCommand DeleteSubscriptionCommand =>
            _DeleteSubscriptionCommand ?? (_DeleteSubscriptionCommand = new RelayCommand(ExecuteDeleteSubscriptionCommand));

        async void ExecuteDeleteSubscriptionCommand()
        {
            var result = await _dialogService.ShowMessageDialog(
                _source.Label,
                "StopSubscribe?".Translate(),
                "StopSubscribe".Translate(),
                "Cancel".Translate()
                );
            if (result)
            {
                _subscriptionManager.RemoveSubscription(_source);
            }
        }


        



        private RelayCommand _MoveToPreviewCommand;
        public RelayCommand MoveToPreviewCommand =>
            _MoveToPreviewCommand ?? (_MoveToPreviewCommand = new RelayCommand(ExecuteMoveToPreviewCommand));

        void ExecuteMoveToPreviewCommand()
        {
            var index = _pageViewModel.Subscriptions.IndexOf(this);
            if (index - 1 >= 0)
            {
                _pageViewModel.Subscriptions.Remove(this);
                _pageViewModel.Subscriptions.Insert(index - 1, this);
            }
        }




        private RelayCommand _MoveToNextCommand;
        public RelayCommand MoveToNextCommand =>
            _MoveToNextCommand ?? (_MoveToNextCommand = new RelayCommand(ExecuteMoveToNextCommand));

        void ExecuteMoveToNextCommand()
        {
            var index = _pageViewModel.Subscriptions.IndexOf(this);
            if (index + 1 < _pageViewModel.Subscriptions.Count)
            {
                _pageViewModel.Subscriptions.Remove(this);
                _pageViewModel.Subscriptions.Insert(index + 1, this);
            }
        }


        private RelayCommand _MoveToHeadCommand;
        public RelayCommand MoveToHeadCommand =>
            _MoveToHeadCommand ?? (_MoveToHeadCommand = new RelayCommand(ExecuteMoveToHeadCommand));

        void ExecuteMoveToHeadCommand()
        {
            if (this == _pageViewModel.Subscriptions.FirstOrDefault()) { return; }
            _pageViewModel.Subscriptions.Remove(this);
            _pageViewModel.Subscriptions.Insert(0, this);
        }



        private RelayCommand _MoveToTailCommand;
        public RelayCommand MoveToTailCommand =>
            _MoveToTailCommand ?? (_MoveToTailCommand = new RelayCommand(ExecuteMoveToTailCommand));

        void ExecuteMoveToTailCommand()
        {
            if (this == _pageViewModel.Subscriptions.LastOrDefault()) { return; }

            _pageViewModel.Subscriptions.Remove(this);
            _pageViewModel.Subscriptions.Add(this);
        }


    }
}
