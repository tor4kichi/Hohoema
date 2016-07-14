using NicoPlayerHohoema.ViewModels;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		
	
		public void OpenPage(HohoemaPageType pageType, object parameter = null)
		{
			var oldPageType = CurrentPageType;
			CurrentPageType = pageType;

			if (!NavigationService.Navigate(pageType.ToString(), parameter))
			{
				CurrentPageType = oldPageType;
			}
		}


		/// <summary>
		/// 外部で戻る処理が行われた際にPageManager上での整合性を取ります
		/// </summary>
		public void OnNavigated(HohoemaPageType pageType)
		{
			CurrentPageType = pageType;
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

		public static string PageTypeToTitle(HohoemaPageType pageType)
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
					return "マイリスト一覧";
				case HohoemaPageType.Mylist:
					return "マイリスト";
				case HohoemaPageType.FavoriteAllFeed:
					return "お気に入り一覧";
				case HohoemaPageType.FavoriteFeed:
					return "お気に入り";
				case HohoemaPageType.FavoriteManage:
					return "お気に入り管理";
				case HohoemaPageType.History:
					return "視聴履歴";
				case HohoemaPageType.Search:
					return "検索";
				case HohoemaPageType.CacheManagement:
					return "ダウンロード管理";
				case HohoemaPageType.Settings:
					return "設定";
				case HohoemaPageType.About:
					return "このアプリについて";
				case HohoemaPageType.VideoInfomation:
					return "動画情報";
				case HohoemaPageType.VideoPlayer:
					return "動画プレイヤー";
				case HohoemaPageType.UserInfo:
					return "ユーザー情報";
				case HohoemaPageType.UserVideo:
					return "ユーザー投稿動画一覧";
				case HohoemaPageType.Login:
					return "ログイン";
				default:
					throw new NotSupportedException("not support " + nameof(HohoemaPageType) + "." + pageType.ToString());
			}
		}

		

		public void UpdateTitle(string title)
		{
			PageTitle = title;
		}
	}


	public class PageInfo
	{
		public PageInfo(HohoemaPageType pageType, object parameter = null, string pageTitle = null)
		{
			PageType = pageType;
			Parameter = parameter;
			PageTitle = String.IsNullOrEmpty(pageTitle) ? PageManager.PageTypeToTitle(pageType) : pageTitle;
		}


		/// <summary>
		/// 実際にページナビゲーションが行われた場合はIsVirtualがfalse
		/// ページナビゲーションが行われていない場合はtrue（この場合、ぱんくずリストに表示することが目的）
		/// </summary>
		public bool IsVirtual { get; internal set; }


		public string PageTitle { get; set; }
		public HohoemaPageType PageType { get; set; }
		public object Parameter { get; set; }
	}
	
}
