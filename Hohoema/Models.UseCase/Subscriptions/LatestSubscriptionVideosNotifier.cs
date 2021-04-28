using I18NPortable;
using Microsoft.Toolkit.Uwp.Notifications;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.Services.Page;
using Hohoema.Models.UseCase.NicoVideos;
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

namespace Hohoema.Models.UseCase.Subscriptions
{
    public sealed class LatestSubscriptionVideosNotifier : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly SubscriptionManager _subscriptionManager;
        private readonly NotificationService _notificationService;

        static readonly string OpenSubscriptionManagementPageParam = new LoginRedirectPayload() { RedirectPageType = HohoemaPageType.SubscriptionManagement }.ToParameterString();
        static readonly string PlayWithWatchAfterPlaylistParam = new LoginRedirectPayload() { RedirectPageType = HohoemaPageType.VideoPlayer, RedirectParamter = $"playlist_id={HohoemaPlaylist.QueuePlaylistId}" }.ToParameterString();

        List<SubscriptionFeedUpdateResult> Results = new List<SubscriptionFeedUpdateResult>();

        public LatestSubscriptionVideosNotifier(
            SubscriptionManager subscriptionManager,
            NotificationService notificationService
            )
        {
            _subscriptionManager = subscriptionManager;
            _notificationService = notificationService;

            Observable.FromEventPattern<SubscriptionFeedUpdateResult>(
                h => _subscriptionManager.Updated += h,
                h => _subscriptionManager.Updated -= h
                )
                .Select(x => x.EventArgs)
                .Do(x => Results.Add(x))
                .Throttle(TimeSpan.FromSeconds(5))
                .Subscribe(_ =>
                {
                    var items = Results.ToList();
                    Results.Clear();
                    // 失敗したアイテムの通知
                    if (!InternetConnection.IsInternet())
                    {
                        foreach (var failedItem in items.Where(x => !x.IsSuccessed))
                        {
                            _notificationService.ShowToast(
                            $"Notification_FailedSubscriptionUpdate".Translate(),
                            failedItem.Entity.Label,
                            Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long,
                            //luanchContent: SubscriptionManagementPageParam,
                            toastButtons: new[] {
                            new ToastButton(HohoemaPageType.SubscriptionManagement.Translate(), OpenSubscriptionManagementPageParam)
                            }
                            );
                        }
                    }

                    // 成功した購読ソースのラベルを連結してトーストコンテンツとして表示する
                    var successedItems = items.Where(x => x.IsSuccessed && x.NewVideos.Any());

                    if (!successedItems.Any()) { return; }

                    var newVideoOwnersText = string.Join(" - ", successedItems.Select(x => x.Entity.Label));
                    _notificationService.ShowToast(
                        $"Notification_SuccessAddToWatchLaterWithAddedCount".Translate(successedItems.Sum(x => x.NewVideos.Count)),
                        newVideoOwnersText,
                        Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long,
                        //luanchContent: PlayWithWatchAfterPlaylistParam,
                        toastButtons: new[] {
                            new ToastButton("WatchVideo".Translate(), PlayWithWatchAfterPlaylistParam),
                            new ToastButton(HohoemaPageType.SubscriptionManagement.Translate(), OpenSubscriptionManagementPageParam)
                        }

                        );
                })
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
