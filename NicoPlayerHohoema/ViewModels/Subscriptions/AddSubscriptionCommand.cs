using I18NPortable;
using NicoPlayerHohoema.Database;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Models.Subscriptions;
using NicoPlayerHohoema.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.Subscriptions
{
    public sealed class AddSubscriptionCommand : DelegateCommandBase
    {
        private readonly SubscriptionManager _subscriptionManager;
        private readonly UserProvider _userProvider;
        private readonly ChannelProvider _channelProvider;
        private readonly NotificationService _notificationService;

        public AddSubscriptionCommand(
            SubscriptionManager subscriptionManager,
            UserProvider userProvider,
            ChannelProvider channelProvider,
            NotificationService notificationService
            )
        {
            _subscriptionManager = subscriptionManager;
            _userProvider = userProvider;
            _channelProvider = channelProvider;
            _notificationService = notificationService;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is IVideoContent
                || parameter is IMylist
                || parameter is IChannel
                || parameter is IUser
                ;
        }

        protected override async void Execute(object parameter)
        {
            (string id, SubscriptionSourceType sourceType, string label) result = parameter switch
            {
                IVideoContent videoContent => videoContent.ProviderType switch
                {
                    NicoVideoUserType.User => (id: videoContent.ProviderId, sourceType: SubscriptionSourceType.User, default(string)),
                    NicoVideoUserType.Channel => (id: videoContent.ProviderId, sourceType: SubscriptionSourceType.Channel, default(string)),
                    _ => throw new NotSupportedException()
                },
                IMylist mylist => (id: mylist.Id, sourceType: SubscriptionSourceType.Mylist, mylist.Label),
                IUser user => (id: user.Id, sourceType: SubscriptionSourceType.User, user.Label),
                _ => throw new NotSupportedException(),
            };

            if (string.IsNullOrEmpty(result.label))
            {
                // resolve name
                result.label = result.sourceType switch
                {
                    //SubscriptionSourceType.Mylist => await ResolveMylistName(result.id),
                    SubscriptionSourceType.User => await ResolveUserName(result.id),
                    SubscriptionSourceType.Channel => await ResolveChannelName(result.id),
                    SubscriptionSourceType.Series => await ResolveSeriesName(result.id),
                    SubscriptionSourceType.SearchWithKeyword => result.id,
                    SubscriptionSourceType.SearchWithTag => result.id,
                    _ => throw new NotSupportedException(),
                };
            }

            var subscription = _subscriptionManager.AddSubscription(result.sourceType, result.id, result.label);
            if (subscription != null)
            {
                Debug.WriteLine($"subscription added: {subscription.Id} {subscription.Label} {subscription.Id}" );
                _notificationService.ShowInAppNotification(new InAppNotificationPayload() 
                {
                    ShowDuration = TimeSpan.FromSeconds(7),
                    SymbolIcon = Windows.UI.Xaml.Controls.Symbol.Accept,
                    Content = "Notification_SuccessAddSubscriptionSourceWithLabel".Translate(subscription.Label),
                    IsShowDismissButton = false,
                });
            }
        }

        async Task<string> ResolveUserName(string id)
        {
            var user = await _userProvider.GetUser(id);
            return user.ScreenName;
        }

        async Task<string> ResolveChannelName(string id)
        {
            var info = await _channelProvider.GetChannelInfo(id);
            return info.Name;
        }

        async Task<string> ResolveSeriesName(string id)
        {
            throw new NotImplementedException();
        }
    }
}
