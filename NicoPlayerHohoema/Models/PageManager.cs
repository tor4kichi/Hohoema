using Prism.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NicoPlayerHohoema.Models
{
	public class PageManager : BindableBase
	{
		public INavigationService NavigationService { get; private set; }



		private HohoemaPageType _CurrentPageType;
		public HohoemaPageType CurrentPageType
		{
			get { return _CurrentPageType; }
			set { SetProperty(ref _CurrentPageType, value); }
		}

		private string _PageTitle;
		public string PageTitle
		{
			get { return _PageTitle; }
			set { SetProperty(ref _PageTitle, value); }
		}


		public PageManager(INavigationService ns)
		{
			NavigationService = ns;
			CurrentPageType = HohoemaPageType.RankingCategoryList;
		}
		

		public void OpenPage<NavigateParamType>(HohoemaPageType pageType, NavigateParamType parameter)
		{
			if (NavigationService.Navigate(pageType.ToString(), parameter))
			{
				CurrentPageType = pageType;

				PageTitle = PageTypeToTitle(CurrentPageType);
			}
		}

		public void OpenPage(HohoemaPageType pageType)
		{
			if (NavigationService.Navigate(pageType.ToString(), null))
			{
				CurrentPageType = pageType;

				PageTitle = PageTypeToTitle(CurrentPageType);
			}
		}


		/// <summary>
		/// 画面遷移の履歴を消去します
		/// </summary>
		/// <remarks>
		/// ログイン後にログイン画面の表示履歴を消す時や
		/// ログアウト後にログイン状態中の画面遷移を消すときに利用します。
		/// </remarks>
		public void ClearNavigateHistory()
		{
			NavigationService.ClearHistory();
		}

		public string CurrentDefaultPageTitle()
		{
			return PageTypeToTitle(CurrentPageType);
		}

		public string PageTypeToTitle(HohoemaPageType pageType)
		{
			switch (pageType)
			{
				case HohoemaPageType.Portal:
					return "ポータル";
				case HohoemaPageType.RankingCategoryList:
					return "ランキングカテゴリ一覧";
				case HohoemaPageType.RankingCategory:
					return "カテゴリランキング";
				case HohoemaPageType.UserMylist:
					return "○○さんマイリスト一覧";
				case HohoemaPageType.Mylist:
					return "マイリスト";
				case HohoemaPageType.FavoriteList:
					return "お気に入り一覧";
				case HohoemaPageType.Favorite:
					return "お気に入り";
				case HohoemaPageType.History:
					return "視聴履歴";
				case HohoemaPageType.Search:
					return "検索";
				case HohoemaPageType.Settings:
					return "設定";
				case HohoemaPageType.About:
					return "このアプリについて";
				case HohoemaPageType.VideoInfomation:
					return "動画情報";
				case HohoemaPageType.VideoPlayer:
					return "動画プレイヤー";
				case HohoemaPageType.Login:
					return "ログイン";
				default:
					throw new NotSupportedException("not support " + nameof(HohoemaPageType) + "." + pageType.ToString());
			}
		}
	}
}
