using Microsoft.Toolkit.Uwp.Notifications;
using NicoPlayerHohoema.Models.Subscriptions;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.UseCase.Playlist;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Subscriptions
{
    public sealed class LatestSubscriptionVideosNotifier : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly SubscriptionManager _subscriptionManager;
        private readonly NotificationService _notificationService;

        static readonly string OpenSubscriptionManagementPageParam = new LoginRedirectPayload() { RedirectPageType = HohoemaPageType.SubscriptionManagement }.ToParameterString();
        static readonly string PlayWithWatchAfterPlaylistParam = new LoginRedirectPayload() { RedirectPageType = HohoemaPageType.VideoPlayer, RedirectParamter = $"playlist_id={HohoemaPlaylist.WatchAfterPlaylistId}" }.ToParameterString();

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
                    if (!Models.Helpers.InternetConnection.IsInternet())
                    {
                        foreach (var failedItem in items.Where(x => !x.IsSuccessed))
                        {
                            _notificationService.ShowToast(
                            $"購読の更新に失敗しました",
                            failedItem.Entity.Label,
                            Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long,
                            //luanchContent: SubscriptionManagementPageParam,
                            toastButtons: new[] {
                            new ToastButton("購読管理", OpenSubscriptionManagementPageParam)
                            }
                            );
                        }
                    }

                    // 成功した購読ソースのラベルを連結してトーストコンテンツとして表示する
                    var successedItems = items.Where(x => x.IsSuccessed && x.NewVideos.Any());

                    if (!successedItems.Any()) { return; }

                    var newVideoOwnersText = string.Join(" - ", successedItems.Select(x => x.Entity.Label));
                    _notificationService.ShowToast(
                        $"新着動画 {successedItems.Sum(x => x.NewVideos.Count)} 件を あとで見る に追加しました",
                        newVideoOwnersText,
                        Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long,
                        //luanchContent: PlayWithWatchAfterPlaylistParam,
                        toastButtons: new[] {
                            new ToastButton("視聴する", PlayWithWatchAfterPlaylistParam),
                            new ToastButton("購読管理", OpenSubscriptionManagementPageParam)
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
