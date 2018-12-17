using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using NicoPlayerHohoema.Models.LocalMylist;
using NicoPlayerHohoema.Models.Cache;

namespace NicoPlayerHohoema.Commands
{
    public static class HohoemaCommnadHelper
    {
        public static T Resolve<T>()
        {
            return (App.Current as App).Container.Resolve<T>();
        }

        public static Models.NiconicoSession GetNiconicoSession()
        {
            return (App.Current as App).Container.Resolve<NiconicoSession>();
        }

        public static Models.FollowManager GetFollowManager()
        {
            return (App.Current as App).Container.Resolve<FollowManager>();
        }

        internal static Models.Subscription.SubscriptionManager GetSubscriptionManager()
        {
            return (App.Current as App).Container.Resolve<Models.Subscription.SubscriptionManager>();
        }

        public static HohoemaPlaylist GetHohoemaPlaylist()
        {
            return  (App.Current as App).Container.Resolve<Models.HohoemaPlaylist>();
        }

        public static VideoCacheManager GetVideoCacheManager()
        {
            return (App.Current as App).Container.Resolve<VideoCacheManager>();
        }

        public static Services.Helpers.MylistHelper GetMylistHelper()
        {
            return (App.Current as App).Container.Resolve<Services.Helpers.MylistHelper>();
        }

        public static LocalMylistManager GetLocalMylistManager()
        {
            return (App.Current as App).Container.Resolve<LocalMylistManager>();
        }

        public static UserMylistManager GetUserMylistManager()
        {
            return (App.Current as App).Container.Resolve<UserMylistManager>();
        }

        public static FeedManager GetFeedManager()
        {
            return (App.Current as App).Container.Resolve<Models.FeedManager>();
        }

        public static Services.PageManager GetPageManager()
        {
            return (App.Current as App).Container.Resolve<Services.PageManager>();
        }
        public static Services.DialogService GetDialogService()
        {
            return (App.Current as App).Container.Resolve<Services.DialogService>();
        }

        public static Services.NotificationService GetNotificationService()
        {
            return (App.Current as App).Container.Resolve<Services.NotificationService>();
        }

        public static Services.HohoemaClipboardService GetClipboardService()
        {
            return (App.Current as App).Container.Resolve<Services.HohoemaClipboardService>();
        }
    }
}
