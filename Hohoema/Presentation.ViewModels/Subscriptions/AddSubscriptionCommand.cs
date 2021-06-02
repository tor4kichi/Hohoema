using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.Series;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Presentation.Services;
using I18NPortable;
using Microsoft.AppCenter.Analytics;
using NiconicoToolkit.Video;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Subscriptions
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
            return parameter is IVideoContentProvider
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
                IVideoContentProvider videoContent => videoContent.ProviderType switch
                {
                    OwnerType.User => (id: videoContent.ProviderId, sourceType: SubscriptionSourceType.User, default(string)),
                    OwnerType.Channel => (id: videoContent.ProviderId, sourceType: SubscriptionSourceType.Channel, default(string)),
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
                _notificationService.ShowLiteInAppNotification_Success("Notification_SuccessAddSubscriptionSourceWithLabel".Translate(subscription.Label));

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
