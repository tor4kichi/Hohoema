using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.Commands
{
    public static class HohoemaCommnadHelper
    {
        public static HohoemaApp GetHohoemaApp()
        {
            return (App.Current as App).Container.Resolve<Models.HohoemaApp>();
        }

        public static HohoemaPlaylist GetHohoemaPlaylist()
        {
            return  (App.Current as App).Container.Resolve<Models.HohoemaPlaylist>();
        }

        public static FeedManager GetFeedManager()
        {
            return (App.Current as App).Container.Resolve<Models.FeedManager>();
        }

        public static PageManager GetPageManager()
        {
            return (App.Current as App).Container.Resolve<Models.PageManager>();
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
