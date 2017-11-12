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
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Mntone.Nico2;
using Windows.System;

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

		public string CanNotFollowReason { get; private set; }

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

		// 動画
		public List<CommunityVideoInfoViewModel> CommunityVideoSamples { get; private set; }


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



		public ReactiveProperty<bool> IsFollowCommunity { get; private set; }
		public ReactiveProperty<bool> CanChangeFollowCommunityState { get; private set; }

		bool _NowProcessCommunity;

		public CommunityPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager)
		{
			IsFollowCommunity = new ReactiveProperty<bool>(mode: ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(_CompositeDisposable);
			CanChangeFollowCommunityState = new ReactiveProperty<bool>()
				.AddTo(_CompositeDisposable);

			IsFollowCommunity
				.Where(x => CommunityId != null)
				.Subscribe(async x =>
				{
					if (_NowProcessCommunity) { return; }

					_NowProcessCommunity = true;

					CanChangeFollowCommunityState.Value = false;
					if (x)
					{
						if (await FollowCommunity())
						{
							Debug.WriteLine(CommunityName + "のコミュニティをお気に入り登録しました.");
						}
						else
						{
							// お気に入り登録に失敗した場合は状態を差し戻し
							Debug.WriteLine(CommunityName + "のコミュニティをお気に入り登録に失敗");
							IsFollowCommunity.Value = false;
						}
					}
					else
					{
						if (await UnfollowCommunity())
						{
							Debug.WriteLine(CommunityName + "のコミュニティをお気に入り解除しました.");
						}
						else
						{
							// お気に入り解除に失敗した場合は状態を差し戻し
							Debug.WriteLine(CommunityName + "のコミュニティをお気に入り解除に失敗");
							IsFollowCommunity.Value = true;
						}
					}

					var isAutoJoinAccept = CommunityInfo.IsPublic;
					var isJoinRequireUserInfo = CommunityInfo.option_flag_details.CommunityPrivUserAuth == "1";
					CanChangeFollowCommunityState.Value =
						IsFollowCommunity.Value == true
						|| (HohoemaApp.FollowManager.CanMoreAddFollow(FollowItemType.Community) && isAutoJoinAccept && !isJoinRequireUserInfo);

					_NowProcessCommunity = false;

					UpdateCanNotFollowReason();
				})
				.AddTo(_CompositeDisposable);
		}


		private async Task<bool> FollowCommunity()
		{
			if (CommunityId == null) { return false; }

			var favManager = HohoemaApp.FollowManager;
			var result = await favManager.AddFollow(FollowItemType.Community, CommunityId, CommunityName);

			return result == ContentManageResult.Success || result == ContentManageResult.Exist;
		}

		private async Task<bool> UnfollowCommunity()
		{
			if (CommunityId == null) { return false; }

			var favManager = HohoemaApp.FollowManager;

            try
            {
                var result = await favManager.RemoveFollow(FollowItemType.Community, CommunityId);

                return result == ContentManageResult.Success;
            }
            catch
            {
                return false;
            }


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
					var res = await HohoemaApp.ContentProvider.GetCommunityInfo(CommunityId);

					if (res == null || !res.IsStatusOK) { return; }

					CommunityInfo = res.Community;

					RaisePropertyChanged(nameof(CommunityName));
					RaisePropertyChanged(nameof(IsPublic));
					RaisePropertyChanged(nameof(CommunityDescription));
					RaisePropertyChanged(nameof(IsOfficial));
					RaisePropertyChanged(nameof(MaxUserCount));
					RaisePropertyChanged(nameof(UserCount));
					RaisePropertyChanged(nameof(CommunityLevel));
					RaisePropertyChanged(nameof(CreatedAt));
					RaisePropertyChanged(nameof(ThumbnailUrl));
					RaisePropertyChanged(nameof(TopUrl));



					var detail = await HohoemaApp.ContentProvider.GetCommunityDetail(CommunityId);

					if (detail == null && !detail.IsStatusOK) { return; }

					CommunityDetail = detail.CommunitySammary.CommunityDetail;

					var profileHtmlId = $"{CommunityId}_profile";
					ProfileHtmlFileUri = await Helpers.HtmlFileHelper.PartHtmlOutputToCompletlyHtml(profileHtmlId, CommunityDetail.ProfielHtml);

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


					CurrentLiveInfoList = CommunityDetail.CurrentLiveList.Select(x => new CurrentLiveInfoViewModel(x, HohoemaApp.Playlist))
						.ToList();

					HasCurrentLiveInfo = CurrentLiveInfoList.Count > 0;

					CommunityVideoSamples = new List<CommunityVideoInfoViewModel>();
					foreach (var sampleVideo in CommunityDetail.VideoList)
					{
						var videoInfoVM = new CommunityVideoInfoViewModel(sampleVideo, HohoemaApp.Playlist);

						CommunityVideoSamples.Add(videoInfoVM);
					}
					

					RaisePropertyChanged(nameof(CommunityOwnerName));
					RaisePropertyChanged(nameof(VideoCount));
					RaisePropertyChanged(nameof(PrivilegeDescription));
//					RaisePropertyChanged(nameof(IsJoinAutoAccept));
//					RaisePropertyChanged(nameof(IsJoinWithoutPrivacyInfo));
//					RaisePropertyChanged(nameof(IsCanLiveOnlyPrivilege));
//					RaisePropertyChanged(nameof(IsCanAcceptJoinOnlyPrivilege));
//					RaisePropertyChanged(nameof(IsCanSubmitVideoOnlyPrivilege));

					RaisePropertyChanged(nameof(ProfileHtmlFileUri));
					RaisePropertyChanged(nameof(OwnerUserInfo));
					RaisePropertyChanged(nameof(Tags));
					RaisePropertyChanged(nameof(FutureLiveList));
					RaisePropertyChanged(nameof(NewsList));
					RaisePropertyChanged(nameof(HasNews));
					RaisePropertyChanged(nameof(CurrentLiveInfoList));
					RaisePropertyChanged(nameof(HasCurrentLiveInfo));
					RaisePropertyChanged(nameof(CommunityVideoSamples));


					// お気に入り状態の取得
					_NowProcessCommunity = true;

					var favManager = HohoemaApp.FollowManager;
					IsFollowCommunity.Value = favManager.IsFollowItem(FollowItemType.Community, CommunityId);

					// 
					var isAutoJoinAccept = CommunityInfo.IsPublic;
					var isJoinRequireUserInfo = CommunityInfo.option_flag_details.CommunityPrivUserAuth == "1";
					CanChangeFollowCommunityState.Value =
						IsFollowCommunity.Value == true
						|| (favManager.CanMoreAddFollow(FollowItemType.Community) && isAutoJoinAccept && !isJoinRequireUserInfo);

					_NowProcessCommunity = false;

					UpdateCanNotFollowReason();
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

        private DelegateCommand _OpenCommunityWebPagePageCommand;
        public DelegateCommand OpenCommunityWebPagePageCommand
        {
            get
            {
                return _OpenCommunityWebPagePageCommand
                    ?? (_OpenCommunityWebPagePageCommand = new DelegateCommand(async () =>
                    {
                        await Launcher.LaunchUriAsync(TopUrl);
                    }));
            }
        }

        private DelegateCommand _OpenCommunityVideoListPageCommand;
		public DelegateCommand OpenCommunityVideoListPageCommand
		{
			get
			{
				return _OpenCommunityVideoListPageCommand
					?? (_OpenCommunityVideoListPageCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.CommunityVideo, CommunityId);
					}));
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



		private void UpdateCanNotFollowReason()
		{
			if (HohoemaApp.FollowManager.IsFollowItem(FollowItemType.Community, CommunityId))
			{
				CanNotFollowReason = null;
			}
			else
			{
				if (!CommunityInfo.IsPublic)
				{
					CanNotFollowReason = "参加には承認が必要";
				}
				else if (CommunityInfo.option_flag_details.CommunityPrivUserAuth == "1")
				{
					CanNotFollowReason = "参加には個人情報公開が必要";
				}
				else
				{
					CanNotFollowReason = null;
				}
			}

			RaisePropertyChanged(nameof(CanNotFollowReason));
		}
	}


	public class CurrentLiveInfoViewModel
	{
		public HohoemaPlaylist HohoemaPlaylist { get; private set; }

		public string LiveTitle { get; private set; }
		public string LiveId { get; private set; }

		public CurrentLiveInfoViewModel(CommunityLiveInfo liveInfo, HohoemaPlaylist playlist)
		{
            HohoemaPlaylist = playlist;

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
                        HohoemaPlaylist.PlayLiveVideo(LiveId, LiveTitle);
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
			var uri = await Helpers.HtmlFileHelper.PartHtmlOutputToCompletlyHtml(id, contentHtml);
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

	public class CommunityVideoInfoViewModel
	{
		public CommunityVideo VideoInfo { get; private set; }
		public HohoemaPlaylist HohoemaPlaylist { get; private set; }


		public string Title { get; private set; }


		public CommunityVideoInfoViewModel(CommunityVideo info, HohoemaPlaylist playlist)
		{
			VideoInfo = info;
            HohoemaPlaylist = playlist;

			Title = VideoInfo.Title;
		}



		private DelegateCommand _OpenVideoPageCommand;
		public DelegateCommand OpenVideoPageCommand
		{
			get
			{
				return _OpenVideoPageCommand
					?? (_OpenVideoPageCommand = new DelegateCommand(() =>
					{
                        HohoemaPlaylist.PlayVideo(VideoInfo.VideoId, VideoInfo.Title);
					}));
			}
		}
	}
}
