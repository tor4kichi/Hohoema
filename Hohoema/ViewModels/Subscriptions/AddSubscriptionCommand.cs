using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.User;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.Series;
using Hohoema.Models.Subscriptions;
using Hohoema.Services;
using I18NPortable;
using NiconicoToolkit.Video;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hohoema.ViewModels.Subscriptions
{
    public sealed class AddSubscriptionCommand : CommandBase
    {
        private readonly ILogger _logger;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly UserProvider _userProvider;
        private readonly ChannelProvider _channelProvider;
        private readonly NotificationService _notificationService;
        private readonly SeriesProvider _seriesRepository;

        public AddSubscriptionCommand(
            ILogger logger,
            SubscriptionManager subscriptionManager,
            UserProvider userProvider,
            ChannelProvider channelProvider,
            NotificationService notificationService,
            SeriesProvider seriesRepository
            )
        {
            _logger = logger;
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
            try
            {
                (string id, SubscriptionSourceType sourceType, string label) result = parameter switch
                {
                    IVideoContentProvider videoContent => videoContent.ProviderType switch
                    {
                        OwnerType.User => (id: videoContent.ProviderId, sourceType: SubscriptionSourceType.User, default(string)),
                        OwnerType.Channel => (id: videoContent.ProviderId, sourceType: SubscriptionSourceType.Channel, default(string)),
                        _ => throw new NotSupportedException()
                    },
                    IMylist mylist => (id: mylist.PlaylistId.Id, sourceType: SubscriptionSourceType.Mylist, mylist.Name),
                    IUser user => (id: user.UserId, sourceType: SubscriptionSourceType.User, user.Nickname),
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

                    //Analytics.TrackEvent("Subscription_Added", new Dictionary<string, string>
                    //    {
                    //        { "SourceType", result.sourceType.ToString() }
                    //    });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "購読追加時にエラーが発生しました。");
            }

        }

        async Task<string> ResolveUserName(string id)
        {
            var user = await _userProvider.GetUserInfoAsync(id);
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
            return seriesDetails.Data.Detail.Title;
        }
    }
}
