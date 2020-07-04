﻿using Hohoema.Interfaces;
using Hohoema.Models;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Pages;
using Hohoema.Models.Repository;
using Hohoema.Models.Repository.App;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Repository.Niconico.Community;
using Hohoema.Models.Repository.Niconico.Follow;
using Hohoema.Services;
using Hohoema.ViewModels.Pages;
using Hohoema.UseCase;
using Hohoema.ViewModels.Pages;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;

namespace Hohoema.ViewModels
{
    public class CommunityPageViewModel 
        : HohoemaViewModelBase,
        ICommunity, 
        INavigatedAwareAsync, 
        IPinablePage, 
        ITitleUpdatablePage
	{
        Models.Pages.HohoemaPin IPinablePage.GetPin()
        {
            return new Models.Pages.HohoemaPin()
            {
                Label = CommunityName,
                PageType = HohoemaPageType.Community,
                Parameter = $"id={CommunityId}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.CommunityName);
        }

        public CommunityPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            AppearanceSettingsRepository appearanceSettings,
            PageManager pageManager,
            NiconicoSession niconicoSession,
            CommunityFollowProvider followProvider,
            CommunityProvider communityProvider,
            FollowManager followManager,
            NiconicoFollowToggleButtonService followToggleButtonService
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            AppearanceSettings = appearanceSettings;
            PageManager = pageManager;
            NiconicoSession = niconicoSession;
            FollowProvider = followProvider;
            CommunityProvider = communityProvider;
            FollowToggleButtonService = followToggleButtonService;
        }


        public string CommunityId { get; private set; }

		public CommunityInfo CommunityInfo { get; private set; }


		public string CommunityName => CommunityInfo?.Name;

		public string CommunityDescription => CommunityInfo?.Description;

		public bool IsPublic => CommunityInfo?.IsPublic ?? false;

		public bool IsOfficial => CommunityInfo?.IsOfficial ?? false;

		public int MaxUserCount => CommunityInfo?.UserMax ?? 0;

		public int UserCount => CommunityInfo?.UserCount ?? 0;

		public int CommunityLevel => CommunityInfo?.Level ?? 0;

		public DateTime CreatedAt => CommunityInfo?.CreateTime ?? DateTime.MinValue;

		public string ThumbnailUrl => CommunityInfo?.ThumbnailUrl;

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
		public List<NicoVideoTag> Tags { get; private set; }
		
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


        private bool _IsOwnedCommunity;
        public bool IsOwnedCommunity
        {
            get { return _IsOwnedCommunity; }
            set { SetProperty(ref _IsOwnedCommunity, value); }
        }

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

        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            
            // ナビゲーションパラメータからコミュニティIDを取得
            IsFailed = false;
            try
            {
                NowLoading = true;

                if (parameters.TryGetValue("id", out string id))
                {
                    CommunityId = id;

                    var res = await CommunityProvider.GetCommunityInfo(CommunityId);

                    if (res == null) { return; }

                    CommunityInfo = res;

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



                    var detail = await CommunityProvider.GetCommunityDetail(CommunityId);

                    if (detail == null) { return; }

                    CommunityDetail = detail;

                    ApplicationTheme appTheme;
                    if (_appearanceSettings.Theme == ElementTheme.Dark)
                    {
                        appTheme = ApplicationTheme.Dark;
                    }
                    else if (_appearanceSettings.Theme == ElementTheme.Light)
                    {
                        appTheme = ApplicationTheme.Light;
                    }
                    else
                    {
                        appTheme = Views.Helpers.SystemThemeHelper.GetSystemTheme();
                    }
                    var profileHtmlId = $"{CommunityId}_profile";
                    ProfileHtmlFileUri = await Models.Helpers.HtmlFileHelper.PartHtmlOutputToCompletlyHtml(profileHtmlId, CommunityDetail.ProfielHtml, appTheme);

                    OwnerUserInfo = new UserInfoViewModel(
                        CommunityDetail.OwnerUserName,
                        CommunityDetail.OwnerUserId
                        );

                    IsOwnedCommunity = NiconicoSession.UserId.ToString() == OwnerUserInfo.Id;

                    Tags = CommunityDetail.Tags.Select(x => new NicoVideoTag(x))
                        .ToList();

                    FutureLiveList = CommunityDetail.FutureLiveList.Select(x => new CommunityLiveInfoViewModel(x))
                        .ToList();

                    RecentLiveList = CommunityDetail.RecentLiveList.Select(x => new CommunityLiveInfoViewModel(x))
                        .ToList();

                    NewsList = new List<CommunityNewsViewModel>();
                    foreach (var news in CommunityDetail.NewsList)
                    {
                        var newsVM = await CommunityNewsViewModel.Create(CommunityId, news.Title, news.PostAuthor, news.PostDate, news.ContentHtml, PageManager, AppearanceSettings);
                        NewsList.Add(newsVM);
                    }



                    HasNews = NewsList.Count > 0;


                    CurrentLiveInfoList = CommunityDetail.CurrentLiveList.Select(x => new CurrentLiveInfoViewModel(x, CommunityDetail))
                        .ToList();

                    HasCurrentLiveInfo = CurrentLiveInfoList.Count > 0;

                    CommunityVideoSamples = new List<CommunityVideoInfoViewModel>();
                    foreach (var sampleVideo in CommunityDetail.VideoList)
                    {
                        var videoInfoVM = new CommunityVideoInfoViewModel(sampleVideo);

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


                    // フォロー表示・操作の準備

                    // Note: オーナーコミュニティのフォローを解除＝コミュニティの解散操作となるため注意が必要
                    // 安全管理上、アプリ上でコミュニティの解散は不可の方向に倒して対応したい
                    if (!IsOwnedCommunity)
                    {
                        FollowToggleButtonService.SetFollowTarget(this);
                    }
                    else
                    {
                        FollowToggleButtonService.SetFollowTarget(null);
                    }

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
						PageManager.OpenPageWithId(HohoemaPageType.CommunityVideo, CommunityId);
					}));
			}
		}

		private DelegateCommand<Uri> _ScriptNotifyCommand;
        private readonly AppearanceSettings _appearanceSettings;

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

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public AppearanceSettingsRepository AppearanceSettings { get; }
        public PageManager PageManager { get; }
        public NiconicoSession NiconicoSession { get; }
        public CommunityFollowProvider FollowProvider { get; }
        public CommunityProvider CommunityProvider { get; }
        public NiconicoFollowToggleButtonService FollowToggleButtonService { get; }

        string INiconicoObject.Id => CommunityId;

        string INiconicoObject.Label => CommunityName;

        private void UpdateCanNotFollowReason()
		{
			if (FollowToggleButtonService.IsFollowTarget.Value)
			{
				CanNotFollowReason = null;
			}
			else
			{
				if (!CommunityInfo.IsPublic)
				{
					CanNotFollowReason = "参加には承認が必要";
				}
				else if (CommunityInfo.CommunityPrivUserAuth == "1")
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


	public class CurrentLiveInfoViewModel : ILiveContent
    {
		public string LiveTitle { get; private set; }
		public string LiveId { get; private set; }

		public CurrentLiveInfoViewModel(CommunityLiveInfo liveInfo, CommunityDetail community)
        {
			LiveTitle = liveInfo.LiveTitle;
			LiveId = liveInfo.LiveId;

            ProviderId = community.Id;
            ProviderName = community.Name;
		}

        public string Id => LiveId;

        public string Label => LiveTitle;

        public string ProviderId { get; }

        public string ProviderName { get; }

        public LiveProviderType ProviderType => LiveProviderType.User;
    }


	public class CommunityNewsViewModel
	{
        static public async Task<CommunityNewsViewModel> Create(
			string communityId,
			string title, 
			string authorName, 
			DateTime postAt, 
			string contentHtml,
            PageManager pageManager,
            AppearanceSettingsRepository appearanceSettings
			)
		{
            ApplicationTheme appTheme;
            if (appearanceSettings.Theme == ElementTheme.Dark)
            {
                appTheme = ApplicationTheme.Dark;
            }
            else if (appearanceSettings.Theme == ElementTheme.Light)
            {
                appTheme = ApplicationTheme.Light;
            }
            else
            {
                appTheme = Views.Helpers.SystemThemeHelper.GetSystemTheme();
            }

            var id = $"{communityId}_{postAt.ToString("yy-MM-dd-H-mm")}";
			var uri = await Models.Helpers.HtmlFileHelper.PartHtmlOutputToCompletlyHtml(id, contentHtml, appTheme);
			return new CommunityNewsViewModel(communityId, title, authorName, postAt, uri, pageManager);
		}

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

        public string CommunityId { get; private set; }
		public string Title { get; private set; }
		public string AuthorName { get; private set; }
		public DateTime PostAt { get; private set; }
		public Uri ContentHtmlFileUri { get; private set; }

		public PageManager PageManager { get; private set; }

		


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


	public class UserInfoViewModel : IUser
	{
        public UserInfoViewModel(string name, string id, string iconUrl = null)
        {
            Name = name;
            Id = id;
            IconUrl = iconUrl;
            HasIconUrl = IconUrl != null;
        }

        public string Name { get; private set; }
		public string Id { get; private set; }
		public string IconUrl { get; private set; }
		public bool HasIconUrl { get; private set; }

        string INiconicoObject.Id => Id;

        string INiconicoObject.Label => Name;
    }

	public class CommunityLiveInfoViewModel : ILiveContent
	{
        public CommunityLiveInfoViewModel(LiveInfo info)
        {
            LiveInfo = info;

            LiveId = LiveInfo.LiveId;
            LiveTitle = LiveInfo.LiveId;
            StartTime = LiveInfo.StartTime;
            StreamerName = LiveInfo.StreamerName;
        }



        public LiveInfo LiveInfo { get; private set; }


		public string LiveId { get; private set; }
		public string LiveTitle { get; private set; }
		public string StreamerName { get; private set; }
		public DateTime StartTime { get; private set; }

        public string BroadcasterId => null;

        public string Id => LiveId;

        public string Label => LiveTitle;

        public string ProviderId => null;

        public string ProviderName => StreamerName;

        public LiveProviderType ProviderType => LiveProviderType.User;
    }

	public class CommunityVideoInfoViewModel : HohoemaListingPageItemBase, IVideoContent
    {
        public string Title { get; }

        public string ProviderId => null;

        public string ProviderName => null;

        public Database.NicoVideoUserType ProviderType => Database.NicoVideoUserType.User;

        public string Id { get; }

        public TimeSpan Length => TimeSpan.Zero;

        public DateTime PostedAt => DateTime.MinValue;

        public int ViewCount => 0;

        public int MylistCount => 0;

        public int CommentCount => 0;

        public string ThumbnailUrl { get; }

        public bool IsDeleted { get; set; }

        public CommunityVideoInfoViewModel(CommunityVideo info)
		{
			Title = info.Title;
            Id = info.VideoId;

            Label = info.Title;
            if (info.ThumbnailUrl != null)
            {
                AddImageUrl(info.ThumbnailUrl);
            }
            ThumbnailUrl = info.ThumbnailUrl;
        }

        public CommunityVideoInfoViewModel(RssVideoData rssVideoData)
        {
            Title = rssVideoData.RawTitle;
            Id = rssVideoData.WatchPageUrl.OriginalString.Split('/').Last();
            Label = Title;
        }


        public bool Equals(IVideoContent other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
