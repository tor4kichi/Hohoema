using Mntone.Nico2.Communities.Detail;
using Mntone.Nico2.Communities.Info;
using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Helpers;
using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Models.Domain.Niconico.UserFeature.Follow;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.UseCase;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.Services.Page;
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
using NiconicoSession = Hohoema.Models.Domain.NiconicoSession;
using Hohoema.Models.Domain.Application;
using Hohoema.Presentation.ViewModels.Community;
using Hohoema.Presentation.ViewModels.Niconico.User;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico
{

    public sealed class Community : ICommunity
    {
        public string Id { get; set; }

        public string Label { get; set; }
    }

    public class CommunityPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
	{
        HohoemaPin IPinablePage.GetPin()
        {
            return new HohoemaPin()
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
            AppearanceSettings appearanceSettings,
            PageManager pageManager,
            NiconicoSession niconicoSession,
            CommunityFollowProvider followProvider,
            CommunityProvider communityProvider,
            FollowManager followManager,
            NiconicoFollowToggleButtonService followToggleButtonService
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            _appearanceSettings = appearanceSettings;
            PageManager = pageManager;
            NiconicoSession = niconicoSession;
            FollowProvider = followProvider;
            CommunityProvider = communityProvider;
            FollowToggleButtonService = followToggleButtonService;
        }

        private Community _community;
        public Community Community
        {
            get { return _community; }
            set { SetProperty(ref _community, value); }
        }


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



                    var detail = await CommunityProvider.GetCommunityDetail(CommunityId);

                    if (detail == null && !detail.IsStatusOK) { return; }

                    CommunityDetail = detail.CommunitySammary.CommunityDetail;

                    ApplicationTheme appTheme;
                    if (_appearanceSettings.ApplicationTheme == ElementTheme.Dark)
                    {
                        appTheme = ApplicationTheme.Dark;
                    }
                    else if (_appearanceSettings.ApplicationTheme == ElementTheme.Light)
                    {
                        appTheme = ApplicationTheme.Light;
                    }
                    else
                    {
                        appTheme = Views.Helpers.SystemThemeHelper.GetSystemTheme();
                    }
                    var profileHtmlId = $"{CommunityId}_profile";
                    ProfileHtmlFileUri = await HtmlFileHelper.PartHtmlOutputToCompletlyHtml(profileHtmlId, CommunityDetail.ProfielHtml, appTheme);

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
                        var newsVM = await CommunityNewsViewModel.Create(CommunityId, news.Title, news.PostAuthor, news.PostDate, news.ContentHtml, PageManager, _appearanceSettings);
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


                    Community = new Community() { Id = CommunityId, Label = CommunityName };

                    // フォロー表示・操作の準備

                    // Note: オーナーコミュニティのフォローを解除＝コミュニティの解散操作となるため注意が必要
                    // 安全管理上、アプリ上でコミュニティの解散は不可の方向に倒して対応したい
                    if (!IsOwnedCommunity)
                    {
                        FollowToggleButtonService.SetFollowTarget(Community);
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
        public PageManager PageManager { get; }
        public NiconicoSession NiconicoSession { get; }
        public CommunityFollowProvider FollowProvider { get; }
        public CommunityProvider CommunityProvider { get; }
        public NiconicoFollowToggleButtonService FollowToggleButtonService { get; }

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
}
