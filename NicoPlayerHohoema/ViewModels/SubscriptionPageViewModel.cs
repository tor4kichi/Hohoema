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
using NicoPlayerHohoema.Services.Page;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class SubscriptionPageViewModel : HohoemaViewModelBase
    {
        public SubscriptionPageViewModel(
            SubscriptionManager subscriptionManager,
            Services.PageManager pageManager,
            Services.WatchItLater watchItLater
            )
        {
            SubscriptionManager = subscriptionManager;
            WatchItLater = watchItLater;

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


        public Services.WatchItLater WatchItLater { get; }
        public SubscriptionManager SubscriptionManager { get; }


        public ReactiveProperty<Subscription> SelectedSubscription { get; }

        public AsyncReactiveCommand RefreshSubscriptions { get; }


        // 前回削除された購読IDを保持する
        // これはListViewの入れ替えがNotifyCollectionChangedAction.Move ではなく
        // Add /Removeで行われることに対するワークアラウンドです
        Guid? _prevRemovedSubscriptionId;


        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            if (parameters.TryGetValue<Guid>("id", out Guid id))
            {
                SelectedSubscription.Value = SubscriptionManager.Subscriptions.FirstOrDefault(x => x.Id == id);
            }

            base.OnNavigatedTo(parameters);
        }
    }


}
