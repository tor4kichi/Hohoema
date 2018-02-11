using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
    public sealed class PageTypeToMaterialIconKind : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            MahApps.Metro.IconPacks.PackIconMaterialKind kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Infinity;
            if (value is Models.HohoemaPageType)
            {
                var pageType = (Models.HohoemaPageType)value;
                switch (pageType)
                {
                    case Models.HohoemaPageType.RankingCategoryList:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Flag;
                        break;
                    case Models.HohoemaPageType.RankingCategory:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Flag;
                        break;
                    case Models.HohoemaPageType.UserMylist:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.ViewList;
                        break;
                    case Models.HohoemaPageType.Mylist:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.ViewList;
                        break;
                    case Models.HohoemaPageType.FollowManage:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Star;
                        break;
                    case Models.HohoemaPageType.WatchHistory:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.History;
                        break;
                    case Models.HohoemaPageType.Search:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.SearchWeb;
                        break;
                    case Models.HohoemaPageType.SearchSummary:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.SearchWeb;
                        break;
                    case Models.HohoemaPageType.SearchResultCommunity:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.SearchWeb;
                        break;
                    case Models.HohoemaPageType.SearchResultTag:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.SearchWeb;
                        break;
                    case Models.HohoemaPageType.SearchResultKeyword:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.SearchWeb;
                        break;
                    case Models.HohoemaPageType.SearchResultMylist:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.SearchWeb;
                        break;
                    case Models.HohoemaPageType.SearchResultLive:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.SearchWeb;
                        break;
                    case Models.HohoemaPageType.FeedGroupManage:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Information;
                        break;
                    case Models.HohoemaPageType.FeedGroup:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Alert;
                        break;
                    case Models.HohoemaPageType.FeedVideoList:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Alert;
                        break;
                    case Models.HohoemaPageType.UserInfo:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Account;
                        break;
                    case Models.HohoemaPageType.UserVideo:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.AccountBox;
                        break;
                    case Models.HohoemaPageType.Community:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Group;
                        break;
                    case Models.HohoemaPageType.CommunityVideo:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Group;
                        break;
                    case Models.HohoemaPageType.VideoInfomation:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Video;
                        break;
                    case Models.HohoemaPageType.CacheManagement:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Download;
                        break;
                    case Models.HohoemaPageType.Settings:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Settings;
                        break;
                    case Models.HohoemaPageType.Login:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Login;
                        break;
                    case Models.HohoemaPageType.Splash:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Login;
                        break;
                    case Models.HohoemaPageType.VideoPlayer:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Video;
                        break;
                    case Models.HohoemaPageType.NicoRepo:
                        kind = MahApps.Metro.IconPacks.PackIconMaterialKind.InformationOutline;
                        break;
                    default:
                        break;
                }
            }

            return kind;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
