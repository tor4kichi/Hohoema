using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Subscription;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class SubscriptionPageViewModel : HohoemaViewModelBase
    {

        public SubscriptionManager SubscriptionManager { get; }

        public WatchItLater WatchItLater { get; }

        public ReactiveProperty<Subscription> SelectedSubscription { get; }

        public AsyncReactiveCommand RefreshSubscriptions { get; }


        // 前回削除された購読IDを保持する
        // これはListViewの入れ替えがNotifyCollectionChangedAction.Move ではなく
        // Add /Removeで行われることに対するワークアラウンドです
        Guid? _prevRemovedSubscriptionId;

        public SubscriptionPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
            : base(hohoemaApp, pageManager)
        {
            SubscriptionManager = SubscriptionManager.Instance;

            WatchItLater = WatchItLater.Instance;

            SelectedSubscription = new ReactiveProperty<Subscription>()
                .AddTo(_CompositeDisposable);

            SubscriptionManager.Subscriptions.ObserveAddChanged()
                .Subscribe(item =>
                {
                    if (_prevRemovedSubscriptionId == item.Id) { return; }

                    SelectedSubscription.Value = item;
                })
                .AddTo(_CompositeDisposable);

            SubscriptionManager.Subscriptions.ObserveRemoveChanged()
                .Subscribe(item =>
                {
                    SelectedSubscription.Value = SelectedSubscription.Value == item ? null : SelectedSubscription.Value;

                    _prevRemovedSubscriptionId = item.Id;
                })
                .AddTo(_CompositeDisposable);
        }


        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (viewModelState?.ContainsKey(nameof(SelectedSubscription)) ?? false)
            {
                var id = (Guid)viewModelState[nameof(SelectedSubscription)];
                SelectedSubscription.Value = SubscriptionManager.Subscriptions.FirstOrDefault(x => x.Id == id);
            }

            base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            if (viewModelState != null)
            {
                if (SelectedSubscription.Value != null)
                {
                    viewModelState.Remove(nameof(SelectedSubscription));
                    viewModelState.Add(nameof(SelectedSubscription), SelectedSubscription.Value.Id);
                }
            }

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }
    }

}
