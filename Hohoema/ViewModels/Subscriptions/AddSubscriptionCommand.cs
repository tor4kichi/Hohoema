using I18NPortable;
using Hohoema.Database;
using Hohoema.Models.Subscriptions;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Repository.Niconico.Channel;
using Hohoema.Models.Repository.Niconico.NicoVideo.Series;
using Hohoema.UseCase.Services;
using Hohoema.UseCase.Events;
using Hohoema.Models.Repository;
using Hohoema.Models.Repository.Niconico.Mylist;

namespace Hohoema.ViewModels.Subscriptions
{
    public sealed class AddSubscriptionCommand : DelegateCommandBase
    {
        private readonly IInAppNotificationService _inAppNotificationService;

        private readonly SubscriptionManager _subscriptionManager;
        private readonly UserProvider _userProvider;
        private readonly ChannelProvider _channelProvider;
        private readonly SeriesRepository _seriesRepository;


        public AddSubscriptionCommand(
            SubscriptionManager subscriptionManager,
            UserProvider userProvider,
            ChannelProvider channelProvider,
            IInAppNotificationService inAppNotificationService,
            SeriesRepository seriesRepository
            )
        {
            _subscriptionManager = subscriptionManager;
            _userProvider = userProvider;
            _channelProvider = channelProvider;
            _inAppNotificationService = inAppNotificationService;
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
                _inAppNotificationService.ShowInAppNotification(new InAppNotificationPayload() 
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
            var seriesDetails = await _seriesRepository.GetSeriesVideosAsync(id);
            return seriesDetails.Series.Title;
        }
    }
}
