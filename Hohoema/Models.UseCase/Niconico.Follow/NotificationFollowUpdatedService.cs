using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Presentation.Services;
using I18NPortable;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Hohoema.Models.UseCase.Niconico.Follow
{
    public sealed class NotificationFollowUpdatedService : IDisposable
    {
        public NotificationFollowUpdatedService(
            NotificationService notificationService
            )
        {
            NotificationService = notificationService;
            
            /*
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
                if (!FollowManager.NiconicoSession.IsLoggedIn) { return; }

                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        {
                            var newItem = e.NewItems.Cast<FollowItemInfo>().FirstOrDefault();
                            if (newItem != null)
                            {
                                NotificationService.ShowLiteInAppNotification_Success("FollowAddedNotification_WithItemName".Translate(newItem.Name));
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
                                NotificationService.ShowLiteInAppNotification_Success("FollowRemovedNotification_WithItemName".Translate(item.Name));
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
            */
        }

        IDisposable disposer;

        public NotificationService NotificationService { get; }

        public void Dispose()
        {
            disposer.Dispose();
        }
    }
}
