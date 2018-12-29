using NicoPlayerHohoema.Models;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Services.Notification
{
    public sealed class NotificationFollowUpdatedService : IDisposable
    {
        public NotificationFollowUpdatedService(
            FollowManager followManager,
            NotificationService notificationService
            )
        {
            FollowManager = followManager;
            NotificationService = notificationService;

            disposer = new[]
            {
                FollowManager.Mylist.FollowInfoItems.CollectionChangedAsObservable(),
                FollowManager.User.FollowInfoItems.CollectionChangedAsObservable(),
                FollowManager.Tag.FollowInfoItems.CollectionChangedAsObservable(),
                FollowManager.Community.FollowInfoItems.CollectionChangedAsObservable(),
                FollowManager.Channel.FollowInfoItems.CollectionChangedAsObservable(),
            }
            .Merge()
            .Subscribe(e => 
            {
                if (!FollowManager.IsLoginUserFollowsReady) { return; }

                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        {
                            var newItem = e.NewItems.Cast<FollowItemInfo>().FirstOrDefault();
                            if (newItem != null)
                            {
                                NotificationService.ShowInAppNotification(new InAppNotificationPayload()
                                {
                                    Content = $"「{newItem.Name}」をフォローしました"
                                    , ShowDuration = TimeSpan.FromSeconds(4)
                                });
                            }
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        {
                            var item = e.OldItems.Cast<FollowItemInfo>().FirstOrDefault();
                            if (item != null)
                            {
                                NotificationService.ShowInAppNotification(new InAppNotificationPayload()
                                {
                                    Content = $"「{item.Name}」のフォローを解除しました"
                                    , ShowDuration = TimeSpan.FromSeconds(4)
                                });
                            }
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        break;
                    default:
                        break;
                }
            });
        }

        IDisposable disposer;

        public FollowManager FollowManager { get; }
        public NotificationService NotificationService { get; }

        public void Dispose()
        {
            disposer.Dispose();
        }
    }
}
