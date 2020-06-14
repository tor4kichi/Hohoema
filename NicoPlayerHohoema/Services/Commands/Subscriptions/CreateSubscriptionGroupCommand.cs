using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Services.Helpers;
using NicoPlayerHohoema.Models.Subscription;
using NicoPlayerHohoema.Services;
using I18NPortable;
using NicoPlayerHohoema.UseCase.Playlist;
using System.Reactive.Concurrency;

namespace NicoPlayerHohoema.Commands.Subscriptions
{
    public sealed class CreateSubscriptionGroupCommand : DelegateCommandBase
    {
        private readonly IScheduler _scheduler;

        public CreateSubscriptionGroupCommand(
            SubscriptionManager subscriptionManager,
            DialogService dialogService,
            IScheduler scheduler
            )
        {
            SubscriptionManager = subscriptionManager;
            DialogService = dialogService;
            _scheduler = scheduler;
        }

        public SubscriptionManager SubscriptionManager { get; }
        public DialogService DialogService { get; }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override async void Execute(object parameter)
        {
            var groupName = await DialogService.GetTextAsync("SubscriptionGroup_Create".Translate(), "", validater: (s) => !string.IsNullOrWhiteSpace(s));

            if (groupName == null) { return; }

            _scheduler.Schedule(async () => 
            {
                var subscription = SubscriptionManager.CreateSusbcription(groupName);

                await Task.Delay(250);

                // 順序重要
                // グループ作成して、SubscriptionManager内でアイテム追加の通知ハンドリングが開始されてから
                // 実際にアイテムを追加する
                if (parameter is Models.Subscription.SubscriptionSource source)
                {
                    subscription.Sources.Add(source);
                }
            });
        }
    }
}
