﻿using I18NPortable;
using Hohoema.FixPrism;

using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.UseCase.Subscriptions;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.Extensions;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Application;
using Microsoft.AppCenter.Analytics;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Microsoft.Toolkit.Mvvm.Messaging;
using Hohoema.Models.UseCase;
using NiconicoToolkit.Video;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;

namespace Hohoema.Presentation.ViewModels.Pages.Hohoema.Subscription
{
    public sealed class SubscriptionManagementPageViewModel : HohoemaPageViewModelBase, INavigationAware, IRecipient<SettingsRestoredMessage>, IDisposable
    {
        public ObservableCollection<SubscriptionViewModel> Subscriptions { get; }

        private readonly SubscriptionManager _subscriptionManager;
        private readonly SubscriptionUpdateManager _subscriptionUpdateManager;
        private readonly DialogService _dialogService;
        private readonly PageManager _pageManager;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly VideoPlayWithQueueCommand _VideoPlayWithQueueCommand;
        private readonly IScheduler _scheduler;


        public IReadOnlyReactiveProperty<bool> IsAutoUpdateRunning { get; }
        public IReadOnlyReactiveProperty<DateTime> NextUpdateTime { get; }
        public IReactiveProperty<TimeSpan> AutoUpdateFrequency { get; }
        public IReactiveProperty<bool> IsAutoUpdateEnabled { get; }

        void IRecipient<SettingsRestoredMessage>.Receive(SettingsRestoredMessage message)
        {
            
        }

        public SubscriptionManagementPageViewModel(
            IScheduler scheduler, 
            SubscriptionManager subscriptionManager,
            SubscriptionUpdateManager subscriptionUpdateManager,
            DialogService dialogService,
            PageManager pageManager,
            NicoVideoProvider nicoVideoProvider,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand
            )
        {
            WeakReferenceMessenger.Default.Register<SettingsRestoredMessage>(this);

            Subscriptions = new ObservableCollection<SubscriptionViewModel>();

            Subscriptions.CollectionChangedAsObservable()
                .Throttle(TimeSpan.FromSeconds(0.25))
                .Subscribe(_ =>
                {
                    Subscriptions.ForEach((index, vm) =>
                    {
                        var subscEntity = vm._source;
                        subscEntity.SortIndex = index + 1; // 新規追加時に既存アイテムを後ろにずらして表示したいため+1
                        _subscriptionManager.UpdateSubscription(subscEntity);
                    });
                })
                .AddTo(_CompositeDisposable);

            _subscriptionManager = subscriptionManager;
            _subscriptionUpdateManager = subscriptionUpdateManager;
            _dialogService = dialogService;
            _pageManager = pageManager;
            _nicoVideoProvider = nicoVideoProvider;
            _VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
            _scheduler = scheduler;

            IsAutoUpdateRunning = _subscriptionUpdateManager.ObserveProperty(x => x.IsRunning)
                .ToReadOnlyReactiveProperty(false)
                .AddTo(_CompositeDisposable);
            NextUpdateTime = _subscriptionUpdateManager.ObserveProperty(x => x.NextUpdateTime)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);
            AutoUpdateFrequency = _subscriptionUpdateManager.ToReactivePropertyAsSynchronized(x => x.UpdateFrequency)
                .AddTo(_CompositeDisposable);
            IsAutoUpdateEnabled = _subscriptionUpdateManager.ToReactivePropertyAsSynchronized(x => x.IsAutoUpdateEnabled)
                .AddTo(_CompositeDisposable);

            _subscriptionManager.Added += _subscriptionManager_Added;
            _subscriptionManager.Removed += _subscriptionManager_Removed;
            _subscriptionManager.Updated += _subscriptionManager_Updated;

        }

        

        public override void Dispose()
        {
            _subscriptionManager.Added -= _subscriptionManager_Added;
            _subscriptionManager.Removed -= _subscriptionManager_Removed;
            _subscriptionManager.Updated -= _subscriptionManager_Updated;

            Subscriptions.DisposeAllOrLog("subscription ViewModel dispose error.");

            base.Dispose();
        }

        public override void OnNavigatingTo(INavigationParameters parameters)
        {
            if (!Subscriptions.Any())
            {
                foreach (var subscInfo in _subscriptionManager.GetAllSubscriptionInfo().OrderBy(x => x.entity.SortIndex))
                {
                    var vm = new SubscriptionViewModel(subscInfo.entity, this, _subscriptionManager, _pageManager, _dialogService, _VideoPlayWithQueueCommand);
                    var items = _nicoVideoProvider.GetCachedVideoInfoItems(subscInfo.feedResult.Videos.Select(x => (VideoId)x.VideoId));
                    vm.UpdateFeedResult(items, subscInfo.feedResult.LastUpdatedAt);
                    Subscriptions.Add(vm);
                }
            }

            base.OnNavigatingTo(parameters);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            base.OnNavigatedFrom(parameters);
        }


        NicoVideo ToVideoContent(FeedResultVideoItem item)
        {
            return _nicoVideoProvider.GetCachedVideoInfo(item.VideoId);
        }

        private void _subscriptionManager_Updated(object sender, SubscriptionFeedUpdateResult e)
        {
            var entity = e.Entity;
            var vm = Subscriptions.FirstOrDefault(x => x.SourceType == entity.SourceType && x.SourceParameter == entity.SourceParameter);
            if (vm != null)
            {
                _scheduler.Schedule(() => 
                {
                    vm.UpdateFeedResult(e.Videos, DateTime.Now);
                });
            }
        }


        private void _subscriptionManager_Removed(object sender, SubscriptionSourceEntity e)
        {
            var target = Subscriptions.FirstOrDefault(x => x.SourceType == e.SourceType && x.SourceParameter == e.SourceParameter);
            if (target == null) { return; }

            _scheduler.Schedule(() =>
            {
                target.Dispose();
                Subscriptions.Remove(target);
            });
        }

        private void _subscriptionManager_Added(object sender, SubscriptionSourceEntity e)
        {
            _scheduler.Schedule(() =>
            {
                var vm = new SubscriptionViewModel(e, this, _subscriptionManager, _pageManager, _dialogService, _VideoPlayWithQueueCommand);
                Subscriptions.Insert(0, vm);
            });
        }

        #region PlayAllUnwatched

        private DelegateCommand _PlayAllUnwatchedCommand;
        public DelegateCommand PlayAllUnwatchedCommand =>
            _PlayAllUnwatchedCommand ?? (_PlayAllUnwatchedCommand = new DelegateCommand(ExecutePlayAllUnwatchedCommand, CanExecutePlayAllUnwatchedCommand));

        void ExecutePlayAllUnwatchedCommand()
        {

        }

        bool CanExecutePlayAllUnwatchedCommand()
        {
            return true;
        }

        #endregion

        #region AddSubscriptionSource

        private DelegateCommand _AddSubscriptionSourceCommand;
        public DelegateCommand AddSubscriptionSourceCommand =>
            _AddSubscriptionSourceCommand ?? (_AddSubscriptionSourceCommand = new DelegateCommand(ExecuteAddSubscriptionSourceCommand, CanExecuteAddSubscriptionSourceCommand));

        void ExecuteAddSubscriptionSourceCommand()
        {

        }

        bool CanExecuteAddSubscriptionSourceCommand()
        {
            return true;
        }

        #endregion


        CancellationTokenSource _cancellationTokenSource;

        private DelegateCommand _AllUpdateCommand;
        public DelegateCommand AllUpdateCommand =>
            _AllUpdateCommand ?? (_AllUpdateCommand = new DelegateCommand(ExecuteAllUpdateCommand));

        void ExecuteAllUpdateCommand()
        {
            _scheduler.Schedule(async () =>
            {
                using (_cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                {
                    try
                    {
                        await _subscriptionUpdateManager.UpdateAsync(_cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        
                    }
                }

                _subscriptionUpdateManager.RestartIfTimerNotRunning();
            });

            Analytics.TrackEvent("Subscription_Update");
        }


        private DelegateCommand _CancelUpdateCommand;
        public DelegateCommand CancelUpdateCommand =>
            _CancelUpdateCommand ?? (_CancelUpdateCommand = new DelegateCommand(ExecuteCancelUpdateCommand));

        void ExecuteCancelUpdateCommand()
        {
            _cancellationTokenSource?.Cancel();
        }
    }



    public sealed class SubscriptionViewModel : BindableBase, IDisposable
    {
        public SubscriptionViewModel(
            SubscriptionSourceEntity source,
            SubscriptionManagementPageViewModel pageViewModel,
            SubscriptionManager subscriptionManager,
            PageManager pageManager,
            DialogService dialogService,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand
            )
        {
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

        public ObservableCollection<VideoListItemControlViewModel> Videos { get; } = new ObservableCollection<VideoListItemControlViewModel>();


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
            Videos.DisposeAll();
        }

        internal void UpdateFeedResult(IList<NicoVideo> result, DateTime updatedAt)
        {
            Videos.Clear();

            Videos.AddRange(result.Select(x => new VideoListItemControlViewModel(x)));
            LastUpdatedAt = updatedAt;
        }

        private DelegateCommand _UpdateCommand;
        public DelegateCommand UpdateCommand =>
            _UpdateCommand ?? (_UpdateCommand = new DelegateCommand(ExecuteUpdateCommand));

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
                ErrorTrackingManager.TrackError(e);
            }
            finally
            {
                NowUpdating = false;
            }

            Analytics.TrackEvent("Subscription_Update", new Dictionary<string, string>
                {
                });
            }

        private DelegateCommand _PlayUnwatchVideosCommand;
        public DelegateCommand PlayUnwatchVideosCommand =>
            _PlayUnwatchVideosCommand ?? (_PlayUnwatchVideosCommand = new DelegateCommand(ExecutePlayUnwatchVideosCommandCommand));

        void ExecutePlayUnwatchVideosCommandCommand()
        {

        }

        private DelegateCommand _OpenSourceVideoListPageCommand;
        public DelegateCommand OpenSourceVideoListPageCommand =>
            _OpenSourceVideoListPageCommand ?? (_OpenSourceVideoListPageCommand = new DelegateCommand(ExecuteOpenSourceVideoListPageCommand));

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


        private DelegateCommand _DeleteSubscriptionCommand;
        public DelegateCommand DeleteSubscriptionCommand =>
            _DeleteSubscriptionCommand ?? (_DeleteSubscriptionCommand = new DelegateCommand(ExecuteDeleteSubscriptionCommand));

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

                Analytics.TrackEvent("Subscription_Removed", new Dictionary<string, string>
                    {
                        { "SourceType", _source.SourceType.ToString() }
                    });
            }
        }


        



        private DelegateCommand _MoveToPreviewCommand;
        public DelegateCommand MoveToPreviewCommand =>
            _MoveToPreviewCommand ?? (_MoveToPreviewCommand = new DelegateCommand(ExecuteMoveToPreviewCommand));

        void ExecuteMoveToPreviewCommand()
        {
            var index = _pageViewModel.Subscriptions.IndexOf(this);
            if (index - 1 >= 0)
            {
                _pageViewModel.Subscriptions.Remove(this);
                _pageViewModel.Subscriptions.Insert(index - 1, this);
            }
        }




        private DelegateCommand _MoveToNextCommand;
        public DelegateCommand MoveToNextCommand =>
            _MoveToNextCommand ?? (_MoveToNextCommand = new DelegateCommand(ExecuteMoveToNextCommand));

        void ExecuteMoveToNextCommand()
        {
            var index = _pageViewModel.Subscriptions.IndexOf(this);
            if (index + 1 < _pageViewModel.Subscriptions.Count)
            {
                _pageViewModel.Subscriptions.Remove(this);
                _pageViewModel.Subscriptions.Insert(index + 1, this);
            }
        }


        private DelegateCommand _MoveToHeadCommand;
        public DelegateCommand MoveToHeadCommand =>
            _MoveToHeadCommand ?? (_MoveToHeadCommand = new DelegateCommand(ExecuteMoveToHeadCommand));

        void ExecuteMoveToHeadCommand()
        {
            if (this == _pageViewModel.Subscriptions.FirstOrDefault()) { return; }
            _pageViewModel.Subscriptions.Remove(this);
            _pageViewModel.Subscriptions.Insert(0, this);
        }



        private DelegateCommand _MoveToTailCommand;
        public DelegateCommand MoveToTailCommand =>
            _MoveToTailCommand ?? (_MoveToTailCommand = new DelegateCommand(ExecuteMoveToTailCommand));

        void ExecuteMoveToTailCommand()
        {
            if (this == _pageViewModel.Subscriptions.LastOrDefault()) { return; }

            _pageViewModel.Subscriptions.Remove(this);
            _pageViewModel.Subscriptions.Add(this);
        }


    }
}
