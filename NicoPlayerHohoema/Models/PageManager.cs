using NicoPlayerHohoema.ViewModels;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml.Navigation;

namespace NicoPlayerHohoema.Models
{
	public class PageManager : BindableBase
	{
		

		public static List<HohoemaPageType> IgnoreRecordNavigationStack = new List<HohoemaPageType>
		{
			HohoemaPageType.Login,
			HohoemaPageType.ConfirmWatchHurmfulVideo,
			HohoemaPageType.VideoPlayer,
			HohoemaPageType.LiveVideoPlayer
		};


		public readonly IReadOnlyList<HohoemaPageType> DontNeedMenuPageTypes = new List<HohoemaPageType>
		{
			HohoemaPageType.Login,
			HohoemaPageType.VideoPlayer,
			HohoemaPageType.LiveVideoPlayer,
		};


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




		public PageManager(INavigationService ns)
		{
			NavigationService = ns;
			CurrentPageType = HohoemaPageType.Portal;
		}

		public void OpenPage(Uri uri)
		{
			var path = uri.AbsoluteUri;
			// is mylist url?
			if (path.StartsWith("https://www.nicovideo.jp/mylist/"))
			{
				var mylistId = uri.AbsolutePath.Split('/').Last();
				System.Diagnostics.Debug.WriteLine($"open Mylist: {mylistId}");
				OpenPage(HohoemaPageType.Mylist, mylistId);

				return;
			}


			if (path.StartsWith("https://www.nicovideo.jp/watch/"))
			{
				// is nico video url?
				var videoId = uri.AbsolutePath.Split('/').Last();
				System.Diagnostics.Debug.WriteLine($"open Video: {videoId}");
				OpenPage(HohoemaPageType.VideoPlayer,
					new VideoPlayPayload()
					{
						VideoId = videoId
					}
					.ToParameterString()
					);

				return;
			}

			if (path.StartsWith("https://com.nicovideo.jp/community/"))
			{
				var communityId = uri.AbsolutePath.Split('/').Last();
				OpenPage(HohoemaPageType.Community, communityId);

				return;
			}
		}

		public void OpenPage(HohoemaPageType pageType, object parameter = null)
		{
			HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
			{
				try
				{
					PageNavigating = true;

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
						if (IsIgnoreRecordPageType(oldPageType))
						{
							ForgetLastPage();
						}
					}
				}
				finally
				{
					PageNavigating = false;
				}
			})
			.AsTask()
			.ConfigureAwait(false);
		}

		public bool IsIgnoreRecordPageType(HohoemaPageType pageType)
		{
			return IgnoreRecordNavigationStack.Any(x => x == pageType);
		}

		public void ForgetLastPage()
		{
			NavigationService.RemoveLastPage();
		}


		/// <summary>
		/// 外部で戻る処理が行われた際にPageManager上での整合性を取ります
		/// </summary>
		public async void OnNavigated(NavigatedToEventArgs e)
		{
			if (e.NavigationMode == NavigationMode.Back || e.NavigationMode == NavigationMode.Forward)
			{
				
				if (e.SourcePageType.Name.EndsWith("Page"))
				{
					var pageTypeString = e.SourcePageType.Name.Remove(e.SourcePageType.Name.IndexOf("Page"));

					HohoemaPageType pageType;
					if (Enum.TryParse(pageTypeString, out pageType))
					{
						try
						{
							PageNavigating = true;

							CurrentPageType = pageType;

							await Task.Delay(250);
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

		public static string PageTypeToTitle(HohoemaPageType pageType)
		{
			switch (pageType)
			{
				case HohoemaPageType.Portal:
					return "ホーム";
				case HohoemaPageType.RankingCategoryList:
					return "ランキングカテゴリ一覧";
				case HohoemaPageType.RankingCategory:
					return "カテゴリランキング";
				case HohoemaPageType.UserMylist:
					return "マイリスト一覧";
				case HohoemaPageType.Mylist:
					return "マイリスト";
				case HohoemaPageType.FollowManage:
					return "フォロー";
				case HohoemaPageType.History:
					return "視聴履歴";
				case HohoemaPageType.Search:
					return "検索";
				case HohoemaPageType.CacheManagement:
					return "キャッシュ管理";
				case HohoemaPageType.Settings:
					return "設定";
				case HohoemaPageType.About:
					return "このアプリについて";
				case HohoemaPageType.Feedback:
					return "フィードバック";
				case HohoemaPageType.VideoInfomation:
					return "動画情報";
				case HohoemaPageType.VideoPlayer:
					return "動画プレイヤー";
				case HohoemaPageType.ConfirmWatchHurmfulVideo:
					return "動画視聴の確認";
				case HohoemaPageType.FeedGroupManage:
					return "全てのフィードグループ";
				case HohoemaPageType.FeedGroup:
					return "フィードグループ";
				case HohoemaPageType.FeedVideoList:
					return "フィードの動画一覧";
				case HohoemaPageType.UserInfo:
					return "ユーザー情報";
				case HohoemaPageType.UserVideo:
					return "ユーザー投稿動画一覧";
				case HohoemaPageType.Community:
					return "コミュニティ情報";
				case HohoemaPageType.CommunityVideo:
					return "コミュニティ動画一覧";
				case HohoemaPageType.LiveVideoPlayer:
					return "生放送プレイヤー";
				case HohoemaPageType.Login:
					return "ログイン";
				default:
					throw new NotSupportedException("not support " + nameof(HohoemaPageType) + "." + pageType.ToString());
			}
		}

		public async Task StartNoUIWork(string title, Func<IAsyncAction> actionFactory)
		{
			StartWork?.Invoke(title, 1);

			using (var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
			{
				await actionFactory().AsTask(cancelSource.Token);

				ProgressWork?.Invoke(1);

				await Task.Delay(2000);

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
