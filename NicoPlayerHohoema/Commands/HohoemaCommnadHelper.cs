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
    }
}
