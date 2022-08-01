using I18NPortable;
using Microsoft.Toolkit.Uwp.Notifications;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Presentation.Services;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Helpers;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Application;
using Hohoema.Models.UseCase.Playlist;
using System.Reactive.Concurrency;
using CommunityToolkit.Mvvm.Messaging;

namespace Hohoema.Models.UseCase.Subscriptions
{
    public sealed class LatestSubscriptionVideosNotifier : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IScheduler _scheduler;
        private readonly IMessenger _messenger;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly NotificationService _notificationService;

        static readonly string OpenSubscriptionManagementPageParam = ToastNotificationConstants.MakeOpenPageToastArguments(HohoemaPageType.SubscriptionManagement).ToString();
        static readonly string PlayWithWatchAfterPlaylistParam = ToastNotificationConstants.MakePlayPlaylistToastArguments(Domain.Playlist.PlaylistItemsSourceOrigin.Local, Domain.Playlist.QueuePlaylist.Id.Id).ToString();

        List<SubscFeedVideo> Results = new List<SubscFeedVideo>();

        public LatestSubscriptionVideosNotifier(
            IScheduler scheduler,
            IMessenger messenger,
            SubscriptionManager subscriptionManager,
            NotificationService notificationService
            )
        {
            _scheduler = scheduler;
            _messenger = messenger;
            _subscriptionManager = subscriptionManager;
            _notificationService = notificationService;

            this._disposables.Add(
            _messenger.ObserveMessage<NewSubscFeedVideoMessage>()
                .Select(x => x.Value)
                .Do(x => Results.Add(x))
                .Throttle(TimeSpan.FromSeconds(5))
                .SubscribeOn(_scheduler)
                .Subscribe(_ =>
                {
                    var items = Results.ToList();
                    Results.Clear();

                    // 成功した購読ソースのラベルを連結してトーストコンテンツとして表示する
                    var newFeedSubscIds = items.Select(x => x.SourceSubscId).Distinct().ToArray();

                    if (!newFeedSubscIds.Any()) { return; }

                    var subscList = newFeedSubscIds.Select(x => _subscriptionManager.getSubscriptionSourceEntity(x));

                    var newVideoOwnersText = string.Join(" - ", subscList.Select(x => x.Label));
                    _notificationService.ShowToast(
                        $"Notification_SuccessAddToWatchLaterWithAddedCount".Translate(items.Count),
                        newVideoOwnersText,
                        Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long,
                        //luanchContent: PlayWithWatchAfterPlaylistParam,
                        toastButtons: new[] {
                                new ToastButton("WatchVideo".Translate(), PlayWithWatchAfterPlaylistParam),
                                new ToastButton(HohoemaPageType.SubscriptionManagement.Translate(), OpenSubscriptionManagementPageParam)
                        }

                        );
                }));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
