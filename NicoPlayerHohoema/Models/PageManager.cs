using NicoPlayerHohoema.Helpers;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.ViewModels;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace NicoPlayerHohoema.Models
{
	public class PageManager : BindableBase
	{
		

		public static readonly HashSet<HohoemaPageType> IgnoreRecordNavigationStack = new HashSet<HohoemaPageType>
		{
            HohoemaPageType.Splash,
            HohoemaPageType.Login,
		};


		public static readonly HashSet<HohoemaPageType> DontNeedMenuPageTypes = new HashSet<HohoemaPageType>
		{
            HohoemaPageType.Splash,
            HohoemaPageType.Login,
		};

        public static bool IsHiddenMenuPage(HohoemaPageType pageType)
        {
            return DontNeedMenuPageTypes.Contains(pageType);
        }

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


		private bool _PageNavigating;
		public bool PageNavigating
		{
			get { return _PageNavigating; }
			set { SetProperty(ref _PageNavigating, value); }
		}

        public HohoemaApp HohoemaApp { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; private set; }
        public AppearanceSettings AppearanceSettings { get; }
        public HohoemaViewManager HohoemaViewManager { get; }
        HohoemaDialogService _HohoemaDialogService;


        private AsyncLock _NavigationLock = new AsyncLock();

        public PageManager(HohoemaApp hohoemaApp, INavigationService ns, AppearanceSettings appearanceSettings, HohoemaPlaylist playlist, HohoemaViewManager viewMan, HohoemaDialogService dialogService)
		{
            HohoemaApp = hohoemaApp;
            NavigationService = ns;
            AppearanceSettings = appearanceSettings;
            HohoemaPlaylist = playlist;
            HohoemaViewManager = viewMan;
            _HohoemaDialogService = dialogService;


            CurrentPageType = HohoemaPageType.RankingCategoryList;
        }

        public void OpenPage(Uri uri)
		{
			var path = uri.AbsoluteUri;
			// is mylist url?
			if (path.StartsWith("https://www.nicovideo.jp/mylist/"))
			{
				var mylistId = uri.AbsolutePath.Split('/').Last();
				System.Diagnostics.Debug.WriteLine($"open Mylist: {mylistId}");
				OpenPage(HohoemaPageType.Mylist, new MylistPagePayload(mylistId).ToParameterString());
				return;
			}


			if (path.StartsWith("https://www.nicovideo.jp/watch/"))
			{
				// is nico video url?
				var videoId = uri.AbsolutePath.Split('/').Last();
				System.Diagnostics.Debug.WriteLine($"open Video: {videoId}");
                HohoemaPlaylist.PlayVideo(videoId);

				return;
			}

			if (path.StartsWith("https://com.nicovideo.jp/community/"))
			{
				var communityId = uri.AbsolutePath.Split('/').Last();
				OpenPage(HohoemaPageType.Community, communityId);

				return;
			}

            if (path.StartsWith("https://com.nicovideo.jp/user/"))
            {
                var userId = uri.AbsolutePath.Split('/').Last();
                OpenPage(HohoemaPageType.UserInfo, userId);

                return;
            }

            Debug.WriteLine($"Urlを処理できませんでした : " + uri.OriginalString);
		}

		public void OpenPage(HohoemaPageType pageType, object parameter = null, bool isForgetNavigation = false)
		{
            HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                using (var releaser = await _NavigationLock.LockAsync())
                {
                    PageNavigating = true;

                    try
                    {
                        await Task.Delay(30);

                        var oldPageTitle = PageTitle;
                        PageTitle = "";
                        var oldPageType = CurrentPageType;
                        CurrentPageType = pageType;

                        await Task.Delay(30);

                        if (!NavigationService.Navigate(pageType.ToString(), parameter))
                        {
                            CurrentPageType = oldPageType;
                            PageTitle = oldPageTitle;
                        }
                        else
                        {
                            if (isForgetNavigation || IsIgnoreRecordPageType(oldPageType))
                            {
                                ForgetLastPage();
                            }
                        }

                    }
                    finally
                    {
                        PageNavigating = false;
                    }

                    HohoemaViewManager.ShowMainView();
                }
            })
            .AsTask()
            .ConfigureAwait(false);
        }

		public bool IsIgnoreRecordPageType(HohoemaPageType pageType)
		{
			return IgnoreRecordNavigationStack.Contains(pageType);
		}

		public void ForgetLastPage()
		{
			NavigationService.RemoveLastPage();
		}


		/// <summary>
		/// 外部で戻る処理が行われた際にPageManager上での整合性を取ります
		/// </summary>
		public void OnNavigated(NavigatedToEventArgs e)
		{
			if (e.NavigationMode == NavigationMode.Back || e.NavigationMode == NavigationMode.Forward)
			{
                string pageTypeString = null;

                if (e.SourcePageType.Name.EndsWith("Page"))
                {
                    pageTypeString = e.SourcePageType.Name.Remove(e.SourcePageType.Name.IndexOf("Page"));
                }
                if (e.SourcePageType.Name.EndsWith("Page_TV"))
                {
                    pageTypeString = e.SourcePageType.Name.Remove(e.SourcePageType.Name.IndexOf("Page_TV"));
                }

                if (pageTypeString != null)
                { 
                    HohoemaPageType pageType;
					if (Enum.TryParse(pageTypeString, out pageType))
					{
						try
						{
							PageNavigating = true;

							CurrentPageType = pageType;
						}
						finally
						{
							PageNavigating = false;
						}

						System.Diagnostics.Debug.WriteLine($"navigated : {pageType.ToString()}");
					}
					else
					{
						throw new NotSupportedException();
					}
				}
				else
				{
					throw new Exception();
				}
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


        public void OpenStartupPage()
        {
            if (Models.AppUpdateNotice.HasNotCheckedUptedeNoticeVersion)
            {
                try
                {
                    HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        await _HohoemaDialogService.ShowLatestUpdateNotice();
                        Models.AppUpdateNotice.UpdateLastCheckedVersionInCurrentVersion();
                    })
                    .AsTask()
                    .ConfigureAwait(false);
                }
                catch { }
            }

            if (HohoemaApp.IsLoggedIn)
            {
                if (IsIgnoreRecordPageType(AppearanceSettings.StartupPageType))
                {
                    AppearanceSettings.StartupPageType = HohoemaPageType.RankingCategoryList;
                }

                OpenPage(AppearanceSettings.StartupPageType);
            }
            else
            {
                OpenPage(HohoemaPageType.CacheManagement);
            }

            
        }

		public static string PageTypeToTitle(HohoemaPageType pageType)
		{
            return Helpers.CulturelizeHelper.ToCulturelizeString(pageType);
		}

		public async Task StartNoUIWork(string title, Func<IAsyncAction> actionFactory)
		{
			StartWork?.Invoke(title, 1);

			using (var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
			{
				await actionFactory().AsTask(cancelSource.Token);

				ProgressWork?.Invoke(1);

				await Task.Delay(1000);

				if (cancelSource.IsCancellationRequested)
				{
					CancelWork?.Invoke();
				}
				else
				{
					CompleteWork?.Invoke();
				}
			}
		}
		public async Task StartNoUIWork(string title, int totalCount, Func<IAsyncActionWithProgress<uint>> actionFactory)
		{
			StartWork?.Invoke(title, (uint)totalCount);

			var progressHandler = new Progress<uint>((x) => ProgressWork?.Invoke(x));

			using (var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
			{
				await actionFactory().AsTask(cancelSource.Token, progressHandler);

				await Task.Delay(500);

				if (cancelSource.IsCancellationRequested)
				{
					CancelWork?.Invoke();
				}
				else 
				{
					CompleteWork?.Invoke();
				}
			}
		}

		

		public void UpdateTitle(string title)
		{
			PageTitle = title;
		}

		public event StartExcludeUserInputWorkHandler StartWork;
		public event ProgressExcludeUserInputWorkHandler ProgressWork;
		public event CompleteExcludeUserInputWorkHandler CompleteWork;
		public event CancelExcludeUserInputWorkHandler CancelWork;
	}


	public delegate void StartExcludeUserInputWorkHandler(string title, uint totalCount);
	public delegate void ProgressExcludeUserInputWorkHandler(uint count);
	public delegate void CompleteExcludeUserInputWorkHandler();
	public delegate void CancelExcludeUserInputWorkHandler();




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
