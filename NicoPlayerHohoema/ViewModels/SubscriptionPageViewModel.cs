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
                    SelectedSubscription.Value = item;
                })
                .AddTo(_CompositeDisposable);

            SubscriptionManager.Subscriptions.ObserveRemoveChanged()
                .Subscribe(item =>
                {
                    SelectedSubscription.Value = SelectedSubscription.Value == item ? null : SelectedSubscription.Value;
                })
                .AddTo(_CompositeDisposable);
        }
    }

}
