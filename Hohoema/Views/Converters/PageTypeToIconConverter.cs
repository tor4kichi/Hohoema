using Hohoema.Models.PageNavigation;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Converters;

public sealed class PageTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is HohoemaPageType pageType)
        {
            return pageType switch
            {
                HohoemaPageType.RankingCategoryList => new SymbolIcon(Symbol.Flag),
                HohoemaPageType.RankingCategory => new SymbolIcon(Symbol.Flag),
                HohoemaPageType.UserMylist => new SymbolIcon(Symbol.List),
                HohoemaPageType.Mylist => new SymbolIcon(Symbol.List),
                HohoemaPageType.FollowManage => new SymbolIcon(Symbol.Favorite),
                HohoemaPageType.WatchHistory => new SymbolIcon(Symbol.Clock),
                HohoemaPageType.Search => new SymbolIcon(Symbol.Find),
                HohoemaPageType.UserInfo => new SymbolIcon(Symbol.Account),
                HohoemaPageType.UserVideo => new SymbolIcon(Symbol.Account),
                HohoemaPageType.Community => new SymbolIcon(Symbol.People),
                HohoemaPageType.CommunityVideo => new SymbolIcon(Symbol.People),
                HohoemaPageType.VideoInfomation => new SymbolIcon(Symbol.Priority),
                HohoemaPageType.CacheManagement => new SymbolIcon(Symbol.Download),
                HohoemaPageType.Timeshift => new SymbolIcon(Symbol.Clock),
                HohoemaPageType.Subscription => new SymbolIcon(Symbol.Important),
                HohoemaPageType.LocalPlaylist => new SymbolIcon(Symbol.List),
                HohoemaPageType.VideoQueue => new SymbolIcon(Symbol.Play),
                HohoemaPageType.NicoRepo => new SymbolIcon(Symbol.Bookmarks),
                HohoemaPageType.SubscriptionManagement => new SymbolIcon(Symbol.Globe),
                HohoemaPageType.SubscVideoList => new SymbolIcon(Symbol.Globe),
                _ => throw new NotSupportedException(),
            };
        }

        throw new NotSupportedException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
