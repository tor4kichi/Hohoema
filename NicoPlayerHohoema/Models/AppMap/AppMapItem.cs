using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace NicoPlayerHohoema.Models.AppMap
{
	public interface IAppMapItem
	{
		string PrimaryLabel { get; }
		string SecondaryLabel { get; }

		HohoemaPageType PageType { get; }
		string Parameter { get; }
	}

	public static class AppMapItemHelper
	{
		public static Windows.UI.Color ToHohoemaPageTypeToDefaultColor(this HohoemaPageType pageType)
		{
			Color color = Colors.Transparent;
			switch (pageType)
			{
				case HohoemaPageType.Portal:
					break;
				case HohoemaPageType.RankingCategoryList:
					break;
				case HohoemaPageType.RankingCategory:
					break;
				case HohoemaPageType.UserMylist:
					break;
				case HohoemaPageType.Mylist:
					break;
				case HohoemaPageType.FavoriteManage:
					break;
				case HohoemaPageType.History:
					break;
				case HohoemaPageType.Search:
					break;
				case HohoemaPageType.CacheManagement:
					break;
				case HohoemaPageType.Settings:
					break;
				case HohoemaPageType.About:
					break;
				case HohoemaPageType.VideoInfomation:
					break;
				case HohoemaPageType.VideoPlayer:
					break;
				case HohoemaPageType.ConfirmWatchHurmfulVideo:
					break;
				case HohoemaPageType.FeedGroupManage:
					break;
				case HohoemaPageType.FeedGroup:
					break;
				case HohoemaPageType.FeedVideoList:
					break;
				case HohoemaPageType.UserInfo:
					break;
				case HohoemaPageType.UserVideo:
					break;
				case HohoemaPageType.Login:
					break;
				default:
					throw new NotSupportedException();
			}

			return color;
		}

	}


}
