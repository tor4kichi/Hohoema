using I18NPortable;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Presentation.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Models.Domain.Niconico;
using Microsoft.AppCenter.Analytics;

namespace Hohoema.Presentation.ViewModels.Subscriptions.Commands
{
    public sealed class AddSubscriptionCommand : DelegateCommandBase
    {
        private readonly SubscriptionManager _subscriptionManager;
        private readonly UserProvider _userProvider;
        private readonly ChannelProvider _channelProvider;
        private readonly NotificationService _notificationService;
        private readonly SeriesRepository _seriesRepository;

        public AddSubscriptionCommand(
            SubscriptionManager subscriptionManager,
            UserProvider userProvider,
            ChannelProvider channelProvider,
            NotificationService notificationService,
            SeriesRepository seriesRepository
            )
        {
            _subscriptionManager = subscriptionManager;
            _userProvider = userProvider;
            _channelProvider = channelProvider;
            _notificationService = notificationService;
            _seriesRepository = seriesRepository;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is IVideoContent
                || parameter is IMylist
                || parameter is IChannel
                || parameter is IUser
                || parameter is ISeries
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
                ISeries series => (id: series.Id.ToString(), sourceType: SubscriptionSourceType.Series, series.Title),
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

                Analytics.TrackEvent("Subscription_Added", new Dictionary<string, string>
                    {
                        { "SourceType", result.sourceType.ToString() }
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
            var seriesDetails = await _seriesRepository.GetSeriesVideosAsync(id);
            return seriesDetails.Series.Title;
        }
    }
}
