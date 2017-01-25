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

        void SelectedAction();

//		HohoemaPageType PageType { get; }
		string Parameter { get; }
	}

	public static class AppMapItemHelper
	{
		public static Windows.UI.Color ToHohoemaPageTypeToDefaultColor(this HohoemaPageType pageType)
		{
			Color color = Colors.DimGray;
			switch (pageType)
			{
				case HohoemaPageType.Portal:
					break;
				case HohoemaPageType.RankingCategoryList:
					color = Util.ColorExtention.HexStringToColor("#ffe8e8");
					break;
				case HohoemaPageType.RankingCategory:
					break;
				case HohoemaPageType.UserMylist:
					color = Util.ColorExtention.HexStringToColor("#e1faf3");
					break;
				case HohoemaPageType.Mylist:
					
					break;
				case HohoemaPageType.FollowManage:
					color = Util.ColorExtention.HexStringToColor("#f7edd3");
					break;
				case HohoemaPageType.History:
					color = Util.ColorExtention.HexStringToColor("#e5effa");
					break;
				case HohoemaPageType.Search:
					color = Util.ColorExtention.HexStringToColor("#e8e0ef");
					break;
				case HohoemaPageType.CacheManagement:
					color = Util.ColorExtention.HexStringToColor("#c8f0ff");
					break;
				case HohoemaPageType.Settings:
					break;
				case HohoemaPageType.About:
					break;
				case HohoemaPageType.VideoInfomation:
					break;
				case HohoemaPageType.ConfirmWatchHurmfulVideo:
					break;
				case HohoemaPageType.FeedGroupManage:
					color = Util.ColorExtention.HexStringToColor("#e8f7d8");
					break;
				case HohoemaPageType.FeedGroup:
					break;
				case HohoemaPageType.FeedVideoList:
					break;
				case HohoemaPageType.UserInfo:
					break;
				case HohoemaPageType.UserVideo:
					break;
				default:
					throw new NotSupportedException();
			}

			return color;
		}

	}


}
