using NicoPlayerHohoema.FixPrism;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Subscriptions;
using NicoPlayerHohoema.UseCase.Subscriptions;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.Extensions;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class SubscriptionManagementPageViewModel : HohoemaViewModelBase, INavigationAware, IDestructible
    {
        public ObservableCollection<SubscriptionViewModel> Subscriptions { get; }

        public Windows.UI.Xaml.Data.CollectionViewSource CollectionViewSource { get; }
        public SubscriptionManagementPageViewModel(
            SubscriptionManager subscriptionManager,
            SubscriptionUpdateManager subscriptionUpdateManager,
            IScheduler scheduler
            )
        {
            Subscriptions = new ObservableCollection<SubscriptionViewModel>();

            CollectionViewSource = new Windows.UI.Xaml.Data.CollectionViewSource()
            { 
            
            };


            _IsSubscriptionUpdateEnabled = new ReactiveProperty<bool>(true);
            IsSubscriptionUpdateEnabled = _IsSubscriptionUpdateEnabled.ToReadOnlyReactiveProperty();
            _subscriptionManager = subscriptionManager;
            _subscriptionUpdateManager = subscriptionUpdateManager;
            _scheduler = scheduler;
        }


        public override void Destroy()
        {
            Subscriptions.DisposeAllOrLog("subscription ViewModel dispose error.");
            base.Destroy();
        }

        public override void OnNavigatingTo(INavigationParameters parameters)
        {
            if (!Subscriptions.Any())
            {
                foreach (var subscInfo in _subscriptionManager.GetAllSubscriptionInfo())
                {
                    var vm = new SubscriptionViewModel(subscInfo.entity, _subscriptionManager);
                    vm.UpdateFeedResult(subscInfo.feedResult.Videos.Select(ToVideoContent).ToList(), subscInfo.feedResult.LastUpdatedAt);
                    Subscriptions.Add(vm);
                }
            }

            _subscriptionManager.Added += _subscriptionManager_Added;
            _subscriptionManager.Removed += _subscriptionManager_Removed;
            _subscriptionManager.Updated += _subscriptionManager_Updated;

            base.OnNavigatingTo(parameters);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            _subscriptionManager.Added -= _subscriptionManager_Added;
            _subscriptionManager.Removed -= _subscriptionManager_Removed;
            _subscriptionManager.Updated -= _subscriptionManager_Updated;

            base.OnNavigatedFrom(parameters);
        }


        static IVideoContent ToVideoContent(FeedResultVideoItem item)
        {
            return Database.NicoVideoDb.Get(item.VideoId);
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
                Subscriptions.Remove(target);
            });
        }

        private void _subscriptionManager_Added(object sender, SubscriptionSourceEntity e)
        {
            _scheduler.Schedule(() =>
            {
                var vm = new SubscriptionViewModel(e, _subscriptionManager);
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


        #region UpdateEnabled

        private readonly ReactiveProperty<bool> _IsSubscriptionUpdateEnabled;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly SubscriptionUpdateManager _subscriptionUpdateManager;
        private readonly IScheduler _scheduler;

        public IReadOnlyReactiveProperty<bool> IsSubscriptionUpdateEnabled { get; }

        private DelegateCommand _ToggleEnableSubscriptionUpdateCommand;
        public DelegateCommand ToggleEnableSubscriptionUpdateCommand =>
            _ToggleEnableSubscriptionUpdateCommand ?? (_ToggleEnableSubscriptionUpdateCommand = new DelegateCommand(ExecuteToggleEnableSubscriptionUpdateCommand));

        void ExecuteToggleEnableSubscriptionUpdateCommand()
        {

        }

        #endregion

        CancellationTokenSource _cancellationTokenSource;

        private DelegateCommand _AllUpdateCommand;
        public DelegateCommand AllUpdateCommand =>
            _AllUpdateCommand ?? (_AllUpdateCommand = new DelegateCommand(ExecuteAllUpdateCommand));

        void ExecuteAllUpdateCommand()
        {
            using (_cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
            {
                _scheduler.Schedule(async () =>
                {
                    try
                    {
                        await _subscriptionUpdateManager.UpdateAsync(_cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        
                    }
                });
            }
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
        public SubscriptionViewModel(SubscriptionSourceEntity source, SubscriptionManager subscriptionManager)
        {
            _source = source;
            _subscriptionManager = subscriptionManager;

            IsEnabled = new ReactiveProperty<bool>(_source.IsEnabled)
                .AddTo(_disposables);
            IsEnabled.Subscribe(isEnabled => 
            {
                _source.IsEnabled = isEnabled;
                _subscriptionManager.UpdateSubscription(_source);
                // TODO: Dispose
            })
                .AddTo(_disposables);
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly SubscriptionSourceEntity _source;
        private readonly SubscriptionManager _subscriptionManager;

        public string SourceParameter => _source.SourceParameter;
        public SubscriptionSourceType SourceType => _source.SourceType;
        public string Label => _source.Label;

        public ReactiveProperty<bool> IsEnabled { get; }

        public ObservableCollection<IVideoContent> Videos { get; } = new ObservableCollection<IVideoContent>();

        public DateTime LastUpdatedAt { get; private set; }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        internal void UpdateFeedResult(IList<IVideoContent> result, DateTime updatedAt)
        {
            Videos.Clear();
            Videos.AddRange(result);
            LastUpdatedAt = updatedAt;
        }


        private DelegateCommand _OpenSourceVideoListPageCommand;
        public DelegateCommand OpenSourceVideoListPageCommand =>
            _OpenSourceVideoListPageCommand ?? (_OpenSourceVideoListPageCommand = new DelegateCommand(ExecuteOpenSourceVideoListPageCommand));

        void ExecuteOpenSourceVideoListPageCommand()
        {

        }


        private DelegateCommand _DeleteSubscriptionCommand;
        public DelegateCommand DeleteSubscriptionCommand =>
            _DeleteSubscriptionCommand ?? (_DeleteSubscriptionCommand = new DelegateCommand(ExecuteDeleteSubscriptionCommand));

        void ExecuteDeleteSubscriptionCommand()
        {

        }

    }
}
