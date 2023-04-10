#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.PageNavigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Navigations;
public static class NavigationMessengerExtension
{
    static readonly Dictionary<HohoemaPageType, string> _pageTypeToName = Enum.GetValues(typeof(HohoemaPageType))
        .Cast<HohoemaPageType>().Select(x => (x, x + "Page")).ToDictionary(x => x.x, x => x.Item2);
   
    public static async Task<INavigationResult> SendNavigationRequestAsync(this IMessenger messenger, HohoemaPageType pageType, INavigationParameters? parameters = null)
    {
        return await messenger.Send(new NavigationAsyncRequestMessage(new(_pageTypeToName[pageType], parameters)));
    }
}
