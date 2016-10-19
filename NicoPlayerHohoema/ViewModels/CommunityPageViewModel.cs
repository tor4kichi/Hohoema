using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Prism.Windows.Navigation;
using Mntone.Nico2.Communities.Info;
using System.Diagnostics;
using Mntone.Nico2.Communities.Detail;
using Prism.Commands;

namespace NicoPlayerHohoema.ViewModels
{
	public class CommunityPageViewModel : HohoemaViewModelBase
	{

		public string CommunityId { get; private set; }

		public CommunityInfo CommunityInfo { get; private set; }


		public string CommunityName => CommunityInfo?.Name;

		public string CommunityDescription => CommunityInfo?.Description;

		public bool IsPublic => CommunityInfo?.IsPublic ?? false;

		public bool IsOfficial => CommunityInfo?.IsOfficial ?? false;

		public uint MaxUserCount => CommunityInfo?.UserMax ?? 0;

		public uint UserCount => CommunityInfo?.UserCount ?? 0;

		public uint CommunityLevel => CommunityInfo?.Level ?? 0;

		public DateTime CreatedAt => CommunityInfo?.CreateTime ?? DateTime.MinValue;

		public string ThumbnailUrl => CommunityInfo?.Thumbnail;

		public Uri TopUrl => CommunityInfo?.TopUrl != null ? new Uri(CommunityInfo.TopUrl) : null;



		public CommunityDetail CommunityDetail { get; private set; }

		public string CommunityOwnerName => CommunityDetail?.OwnerUserName;

		public uint VideoCount => CommunityDetail?.VideoCount ?? 0;

		public string PrivilegeDescription => CommunityDetail?.PrivilegeDescription;

//		public bool IsJoinAutoAccept => CommunityDetail?.Option.IsJoinAutoAccept ?? false;
//		public bool IsJoinWithoutPrivacyInfo => CommunityDetail?.Option.IsJoinWithoutPrivacyInfo ?? false;
//		public bool IsCanLiveOnlyPrivilege => CommunityDetail?.Option.IsCanLiveOnlyPrivilege ?? false;
//		public bool IsCanAcceptJoinOnlyPrivilege => CommunityDetail?.Option.IsCanAcceptJoinOnlyPrivilege ?? false;
//		public bool IsCanSubmitVideoOnlyPrivilege => CommunityDetail?.Option.IsCanSubmitVideoOnlyPrivilege ?? false;

		// プロフィールHTMLの表示
		public Uri ProfileHtmlFileUri { get; set; }

		// オーナーユーザー
		public UserInfoViewModel OwnerUserInfo { get; private set; }

		// タグ
		public List<TagViewModel> Tags { get; private set; }
		
		// 生放送予定の表示
		public List<CommunityLiveInfoViewModel> FutureLiveList { get; private set; }

		// 生放送予定の表示
		public List<CommunityLiveInfoViewModel> RecentLiveList { get; private set; }


		// コミュニティのお知らせ
		public List<CommunityNewsViewModel> NewsList { get; private set; }

		public bool HasNews { get; private set; }

		// 生放送
		public List<CurrentLiveInfoViewModel> CurrentLiveInfoList { get; private set; }

		public bool HasCurrentLiveInfo { get; private set; }

		private bool _NowLoading;
		public bool NowLoading
		{
			get { return _NowLoading; }
			set { SetProperty(ref _NowLoading, value); }
		}

		private bool _IsFailed;
		public bool IsFailed
		{
			get { return _IsFailed; }
			set { SetProperty(ref _IsFailed, value); }
		}

		public CommunityPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager, isRequireSignIn: true)
		{

		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			// ナビゲーションパラメータからコミュニティIDを取得
			IsFailed = false;
			try
			{
				NowLoading = true;

				CommunityId = null;
				if (e.Parameter is string)
				{
					CommunityId = e.Parameter as string;
				}

				// コミュニティ情報の取得
				if (!string.IsNullOrEmpty(CommunityId))
				{
					var res = await HohoemaApp.ContentFinder.GetCommunityInfo(CommunityId);

					if (res == null || !res.IsStatusOK) { return; }

					CommunityInfo = res.Community;

					OnPropertyChanged(nameof(CommunityName));
					OnPropertyChanged(nameof(IsPublic));
					OnPropertyChanged(nameof(CommunityDescription));
					OnPropertyChanged(nameof(IsOfficial));
					OnPropertyChanged(nameof(MaxUserCount));
					OnPropertyChanged(nameof(UserCount));
					OnPropertyChanged(nameof(CommunityLevel));
					OnPropertyChanged(nameof(CreatedAt));
					OnPropertyChanged(nameof(ThumbnailUrl));
					OnPropertyChanged(nameof(TopUrl));



					var detail = await HohoemaApp.ContentFinder.GetCommunityDetail(CommunityId);

					if (detail == null && !detail.IsStatusOK) { return; }

					CommunityDetail = detail.CommunitySammary.CommunityDetail;

					var profileHtmlId = $"{CommunityId}_profile";
					ProfileHtmlFileUri = await Util.HtmlFileHelper.PartHtmlOutputToCompletlyHtml(profileHtmlId, CommunityDetail.ProfielHtml);

					OwnerUserInfo = new UserInfoViewModel(
						CommunityDetail.OwnerUserName,
						CommunityDetail.OwnerUserId
						);

					Tags = CommunityDetail.Tags.Select(x => new TagViewModel(x, PageManager))
						.ToList();

					FutureLiveList = CommunityDetail.FutureLiveList.Select(x => new CommunityLiveInfoViewModel(x, PageManager))
						.ToList();

					RecentLiveList = CommunityDetail.RecentLiveList.Select(x => new CommunityLiveInfoViewModel(x, PageManager))
						.ToList();

					NewsList = new List<CommunityNewsViewModel>();
					foreach (var news in CommunityDetail.NewsList)
					{
						var newsVM = await CommunityNewsViewModel.Create(CommunityId, news.Title, news.PostAuthor, news.PostDate, news.ContentHtml, PageManager);
						NewsList.Add(newsVM);
					}



					HasNews = NewsList.Count > 0;


					CurrentLiveInfoList = CommunityDetail.CurrentLiveList.Select(x => new CurrentLiveInfoViewModel(x, PageManager))
						.ToList();

					HasCurrentLiveInfo = CurrentLiveInfoList.Count > 0;

					OnPropertyChanged(nameof(CommunityOwnerName));
					OnPropertyChanged(nameof(VideoCount));
					OnPropertyChanged(nameof(PrivilegeDescription));
//					OnPropertyChanged(nameof(IsJoinAutoAccept));
//					OnPropertyChanged(nameof(IsJoinWithoutPrivacyInfo));
//					OnPropertyChanged(nameof(IsCanLiveOnlyPrivilege));
//					OnPropertyChanged(nameof(IsCanAcceptJoinOnlyPrivilege));
//					OnPropertyChanged(nameof(IsCanSubmitVideoOnlyPrivilege));

					OnPropertyChanged(nameof(ProfileHtmlFileUri));
					OnPropertyChanged(nameof(OwnerUserInfo));
					OnPropertyChanged(nameof(Tags));
					OnPropertyChanged(nameof(FutureLiveList));
					OnPropertyChanged(nameof(NewsList));
					OnPropertyChanged(nameof(HasNews));
					OnPropertyChanged(nameof(CurrentLiveInfoList));
					OnPropertyChanged(nameof(HasCurrentLiveInfo));


				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
				IsFailed = true;
			}
			finally
			{
				NowLoading = false;
			}

			// UpdateTitle
			if (!IsFailed)
			{
				UpdateTitle($"コミュニティ");
			}
		}

		private DelegateCommand<Uri> _ScriptNotifyCommand;
		public DelegateCommand<Uri> ScriptNotifyCommand
		{
			get
			{
				return _ScriptNotifyCommand
					?? (_ScriptNotifyCommand = new DelegateCommand<Uri>((parameter) =>
					{
						System.Diagnostics.Debug.WriteLine($"script notified: {parameter}");

						PageManager.OpenPage(parameter);
					}));
			}
		}

	}


	public class CurrentLiveInfoViewModel
	{
		public PageManager PageManager { get; private set; }

		public string LiveTitle { get; private set; }
		public string LiveId { get; private set; }

		public CurrentLiveInfoViewModel(CommunityLiveInfo liveInfo, PageManager pageManager)
		{
			PageManager = pageManager;

			LiveTitle = liveInfo.LiveTitle;
			LiveId = liveInfo.LiveId;
		}

		private DelegateCommand _OpenLivePageCommand;
		public DelegateCommand OpenLivePageCommand
		{
			get
			{
				return _OpenLivePageCommand
					?? (_OpenLivePageCommand = new DelegateCommand(() =>
					{
						// TODO: 生放送ページを開く lv0000000
						var livePagePayload = new Models.Live.LiveVidePagePayload(LiveId)
						{
							LiveTitle = LiveTitle
						};

						PageManager.OpenPage(HohoemaPageType.LiveVideoPlayer, livePagePayload.ToParameterString());
					}));
			}
		}
	}


	public class CommunityNewsViewModel
	{
		public static async Task<CommunityNewsViewModel> Create(
			string communityId,
			string title, 
			string authorName, 
			DateTime postAt, 
			string contentHtml,
			PageManager pageManager
			)
		{
			var id = $"{communityId}_{postAt.ToString("yy-MM-dd-H-mm")}";
			var uri = await Util.HtmlFileHelper.PartHtmlOutputToCompletlyHtml(id, contentHtml);
			return new CommunityNewsViewModel(communityId, title, authorName, postAt, uri, pageManager);
		}


		public string CommunityId { get; private set; }
		public string Title { get; private set; }
		public string AuthorName { get; private set; }
		public DateTime PostAt { get; private set; }
		public Uri ContentHtmlFileUri { get; private set; }

		public PageManager PageManager { get; private set; }

		private CommunityNewsViewModel(
			string communityId,
			string title,
			string authorName,
			DateTime postAt,
			Uri htmlUri,
			PageManager pageManager
			)
		{
			CommunityId = communityId;
			Title = title;
			AuthorName = authorName;
			PostAt = postAt;
			ContentHtmlFileUri = htmlUri;
			PageManager = pageManager;
		}


		private DelegateCommand<Uri> _ScriptNotifyCommand;
		public DelegateCommand<Uri> ScriptNotifyCommand
		{
			get
			{
				return _ScriptNotifyCommand
					?? (_ScriptNotifyCommand = new DelegateCommand<Uri>((parameter) =>
					{
						System.Diagnostics.Debug.WriteLine($"script notified: {parameter}");

						PageManager.OpenPage(parameter);
					}));
			}
		}

	}


	public class UserInfoViewModel
	{
		public string Name { get; private set; }
		public string Id { get; private set; }
		public string IconUrl { get; private set; }
		public bool HasIconUrl { get; private set; }

		public UserInfoViewModel(string name, string id, string iconUrl = null)
		{
			Name = name;
			Id = id;
			IconUrl = iconUrl;
			HasIconUrl = IconUrl != null;
		}
	}

	public class CommunityLiveInfoViewModel
	{
		public LiveInfo LiveInfo { get; private set; }
		public PageManager PageManager { get; private set; }


		public string LiveId { get; private set; }
		public string LiveTitle { get; private set; }
		public string StreamerName { get; private set; }
		public DateTime StartTime { get; private set; }


		public CommunityLiveInfoViewModel(LiveInfo info, PageManager pageManager)
		{
			LiveInfo = info;
			PageManager = pageManager;

			LiveId = LiveInfo.LiveId;
			LiveTitle = LiveInfo.LiveId;
			StartTime = LiveInfo.StartTime;
			StreamerName = LiveInfo.StreamerName;
		}



		private DelegateCommand _OpenLivePageCommand;
		public DelegateCommand OpenLivePageCommand
		{
			get
			{
				return _OpenLivePageCommand
					?? (_OpenLivePageCommand = new DelegateCommand(() => 
					{
						// TODO: 生放送ページを開く lv0000000

					}));
			}
		}
	}
}
