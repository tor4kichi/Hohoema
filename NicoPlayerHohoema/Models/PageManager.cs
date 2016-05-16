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
			}
		}

		public void OpenPage(HohoemaPageType pageType)
		{
			if (NavigationService.Navigate(pageType.ToString(), null))
			{
				CurrentPageType = pageType;
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
	}
}
