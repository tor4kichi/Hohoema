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
            HohoemaPageType.PrologueIntroduction,
            HohoemaPageType.EpilogueIntroduction,
        };


		public static readonly HashSet<HohoemaPageType> DontNeedMenuPageTypes = new HashSet<HohoemaPageType>
		{
            HohoemaPageType.Splash,
            HohoemaPageType.PrologueIntroduction,
            HohoemaPageType.NicoAccountIntroduction,
            HohoemaPageType.VideoCacheIntroduction,
            HohoemaPageType.EpilogueIntroduction,
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

        public object PageNavigationParameter { get; private set; } = -1;


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

        public bool OpenPage(Uri uri)
		{
			var path = uri.AbsoluteUri;
			// is mylist url?
			if (path.StartsWith("http://www.nicovideo.jp/mylist/") || path.StartsWith("https://www.nicovideo.jp/mylist/"))
			{
				var mylistId = uri.AbsolutePath.Split('/').Last();
				System.Diagnostics.Debug.WriteLine($"open Mylist: {mylistId}");
				OpenPage(HohoemaPageType.Mylist, new MylistPagePayload(mylistId).ToParameterString());
				return true;
			}


			if (path.StartsWith("http://www.nicovideo.jp/watch/") || path.StartsWith("https://www.nicovideo.jp/watch/"))
			{
				// is nico video url?
				var videoId = uri.AbsolutePath.Split('/').Last();
				System.Diagnostics.Debug.WriteLine($"open Video: {videoId}");
                HohoemaPlaylist.PlayVideo(videoId);

                return true;
            }

			if (path.StartsWith("http://com.nicovideo.jp/community/") || path.StartsWith("https://com.nicovideo.jp/community/"))
			{
				var communityId = uri.AbsolutePath.Split('/').Last();
				OpenPage(HohoemaPageType.Community, communityId);

                return true;
            }

            if (path.StartsWith("http://com.nicovideo.jp/user/") || path.StartsWith("https://com.nicovideo.jp/user/"))
            {
                var userId = uri.AbsolutePath.Split('/').Last();
                OpenPage(HohoemaPageType.UserInfo, userId);

                return true;
            }

            if (path.StartsWith("http://ch.nicovideo.jp/") || path.StartsWith("https://ch.nicovideo.jp/"))
            {
                var elem = uri.AbsolutePath.Split('/');
                if (elem.Any(x => x == "article" || x == "blomaga"))
                {
                    return false;
                }
                else
                {
                    OpenPage(HohoemaPageType.ChannelVideo, elem.Last());
                }

                return true;
            }

            Debug.WriteLine($"Urlを処理できませんでした : " + uri.OriginalString);

            return false;
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
                        if (CurrentPageType == pageType && PageNavigationParameter == parameter)
                        {
                            return;
                        }

                        await Task.Delay(30);

                        var oldPageTitle = PageTitle;
                        PageTitle = "";
                        var oldPageType = CurrentPageType;
                        CurrentPageType = pageType;
                        var oldPageParameter = PageNavigationParameter;
                        PageNavigationParameter = parameter;

                        await Task.Delay(30);

                        if (!NavigationService.Navigate(pageType.ToString(), parameter))
                        {
                            CurrentPageType = oldPageType;
                            PageTitle = oldPageTitle;
                            PageNavigationParameter = oldPageParameter;
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

                    PageNavigationParameter = e.Parameter;
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


        public void OpenIntroductionPage()
        {
            OpenPage(HohoemaPageType.PrologueIntroduction);
        }

        public void OpenStartupPage()
        {
            try
            {
                HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    if (Models.AppUpdateNotice.HasNotCheckedUptedeNoticeVersion)
                    {
                        await _HohoemaDialogService.ShowLatestUpdateNotice();
                        Models.AppUpdateNotice.UpdateLastCheckedVersionInCurrentVersion();

                    }
                    if (HohoemaApp.IsLoggedIn)
                    {
                        if (IsIgnoreRecordPageType(AppearanceSettings.StartupPageType))
                        {
                            AppearanceSettings.StartupPageType = HohoemaPageType.RankingCategoryList;
                        }

                        OpenPage(AppearanceSettings.StartupPageType);
                    }
                    else if (HohoemaApp.UserSettings.CacheSettings.IsUserAcceptedCache)
                    {
                        OpenPage(HohoemaPageType.CacheManagement);
                    }
                    else if (Helpers.InternetConnection.IsInternet())
                    {
                        OpenPage(HohoemaPageType.RankingCategoryList);
                    }
                    else
                    {
                        OpenPage(HohoemaPageType.Settings);
                    }
                })
                .AsTask()
                .ConfigureAwait(false);
            }
            catch { }
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
