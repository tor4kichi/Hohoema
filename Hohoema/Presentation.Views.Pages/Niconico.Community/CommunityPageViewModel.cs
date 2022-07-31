using Hohoema.Models.Domain;
using Hohoema.Models.Helpers;
using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.UseCase;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.PageNavigation;
using CommunityToolkit.Mvvm.Input;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using NiconicoSession = Hohoema.Models.Domain.Niconico.NiconicoSession;
using Hohoema.Models.Domain.Application;
using Hohoema.Presentation.ViewModels.Community;
using Hohoema.Presentation.ViewModels.Niconico.User;
using Hohoema.Models.Domain.Pins;
using Hohoema.Presentation.ViewModels.Niconico.Follow;
using NiconicoToolkit.Community;
using NiconicoToolkit;
using Hohoema.Presentation.Navigations;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Community
{
    using CommunityFollowContext = FollowContext<ICommunity>;


    public class CommunityPageViewModel : HohoemaPageViewModelBase, IPinablePage, ITitleUpdatablePage
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


        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public NiconicoSession NiconicoSession { get; }
        public CommunityProvider CommunityProvider { get; }
        private readonly AppearanceSettings _appearanceSettings;
        private readonly CommunityFollowProvider _communityFollowProvider;



        public CommunityPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            AppearanceSettings appearanceSettings,
            PageManager pageManager,
            NiconicoSession niconicoSession,
            CommunityFollowProvider communityFollowProvider,
            CommunityProvider communityProvider
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            _appearanceSettings = appearanceSettings;
            PageManager = pageManager;
            NiconicoSession = niconicoSession;
            _communityFollowProvider = communityFollowProvider;
            CommunityProvider = communityProvider;
        }

        private CommunityViewModel _community;
        public CommunityViewModel Community
        {
            get { return _community; }
            set { SetProperty(ref _community, value); }
        }


        public CommunityId? CommunityId { get; private set; }

		public CommunityInfoData CommunityInfo { get; private set; }


		public string CommunityName => CommunityInfo?.Name;

		public string CommunityDescription => CommunityInfo?.Description;

		public bool IsPublic => CommunityInfo?.IsPublic ?? false;

		public int CommunityLevel => CommunityInfo?.Level ?? 0;

		public string ThumbnailUrl => CommunityInfo?.ThumbnailNonSsl?.OriginalString;



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
		//public List<CurrentLiveInfoViewModel> CurrentLiveInfoList { get; private set; }

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

        // Follow
        private CommunityFollowContext _followContext = CommunityFollowContext.Default;
        public CommunityFollowContext FollowContext
        {
            get => _followContext;
            set => SetProperty(ref _followContext, value);
        }

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            await base.OnNavigatedToAsync(parameters);

            CommunityId? communityId = null;
            if (parameters.TryGetValue("id", out string strCommunityId))
            {
                communityId = strCommunityId;
            }
            else if (parameters.TryGetValue("id", out CommunityId justCommunityId))
            {
                communityId = justCommunityId;
            }


            if (communityId == null)
            {
                CommunityId = null;
                CommunityInfo = null;
                FollowContext = CommunityFollowContext.Default;
                return;
            }

            CommunityId = communityId;

            IsFailed = false;
            try
            {
                var res = await CommunityProvider.GetCommunityInfo(CommunityId.Value);

                if (res == null || !res.IsOK) { return; }

                CommunityInfo = res.Community;

                OnPropertyChanged(nameof(CommunityName));
                OnPropertyChanged(nameof(IsPublic));
                OnPropertyChanged(nameof(CommunityDescription));
                OnPropertyChanged(nameof(CommunityLevel));
                OnPropertyChanged(nameof(ThumbnailUrl));

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
                //var profileHtmlId = $"{CommunityId}_profile";
                //ProfileHtmlFileUri = await HtmlFileHelper.PartHtmlOutputToCompletlyHtml(profileHtmlId, CommunityDetail.ProfielHtml, appTheme);


                //OwnerUserInfo = new UserInfoViewModel(
                //    CommunityDetail.OwnerUserName,
                //    CommunityDetail.OwnerUserId
                //    );

                //IsOwnedCommunity = NiconicoSession.UserId.ToString() == OwnerUserInfo.Id;

                //Tags = CommunityDetail.Tags.Select(x => new NicoVideoTag(x))
                //    .ToList();

                //FutureLiveList = CommunityDetail.FutureLiveList.Select(x => new CommunityLiveInfoViewModel(x))
                //    .ToList();

                //RecentLiveList = CommunityDetail.RecentLiveList.Select(x => new CommunityLiveInfoViewModel(x))
                //    .ToList();

                //NewsList = new List<CommunityNewsViewModel>();
                //foreach (var news in CommunityDetail.NewsList)
                //{
                //    var newsVM = await CommunityNewsViewModel.Create(CommunityId, news.Title, news.PostAuthor, news.PostDate, news.ContentHtml, PageManager, _appearanceSettings);
                //    NewsList.Add(newsVM);
                //}



                //HasNews = NewsList.Count > 0;


                //CurrentLiveInfoList = CommunityDetail.CurrentLiveList.Select(x => new CurrentLiveInfoViewModel(x, CommunityDetail))
                //    .ToList();

                //HasCurrentLiveInfo = CurrentLiveInfoList.Count > 0;

                //CommunityVideoSamples = new List<CommunityVideoInfoViewModel>();
                ///*
                //foreach (var sampleVideo in CommunityDetail.VideoList)
                //{
                //    var videoInfoVM = new CommunityVideoInfoViewModel(sampleVideo);

                //    CommunityVideoSamples.Add(videoInfoVM);
                //}
                //*/


                //OnPropertyChanged(nameof(CommunityOwnerName));
                //OnPropertyChanged(nameof(VideoCount));
                //OnPropertyChanged(nameof(PrivilegeDescription));
                //					OnPropertyChanged(nameof(IsJoinAutoAccept));
                //					OnPropertyChanged(nameof(IsJoinWithoutPrivacyInfo));
                //					OnPropertyChanged(nameof(IsCanLiveOnlyPrivilege));
                //					OnPropertyChanged(nameof(IsCanAcceptJoinOnlyPrivilege));
                //					OnPropertyChanged(nameof(IsCanSubmitVideoOnlyPrivilege));

                //OnPropertyChanged(nameof(ProfileHtmlFileUri));
                //OnPropertyChanged(nameof(OwnerUserInfo));
                //OnPropertyChanged(nameof(Tags));
                //OnPropertyChanged(nameof(FutureLiveList));
                //OnPropertyChanged(nameof(NewsList));
                //OnPropertyChanged(nameof(HasNews));
                //OnPropertyChanged(nameof(CurrentLiveInfoList));
                //OnPropertyChanged(nameof(HasCurrentLiveInfo));
                //OnPropertyChanged(nameof(CommunityVideoSamples));


                Community = new CommunityViewModel() { CommunityId = CommunityId.Value, Name = CommunityName };

                // フォロー表示・操作の準備

                // Note: オーナーコミュニティのフォローを解除＝コミュニティの解散操作となるため注意が必要
                // 安全管理上、アプリ上でコミュニティの解散は不可の方向に倒して対応したい
                try
                {
                    FollowContext = CommunityFollowContext.Default;
                    var authority = await _communityFollowProvider.GetCommunityAuthorityAsync(CommunityId.Value);
                    if (!authority.Data.IsOwner)
                    {
                        FollowContext = await CommunityFollowContext.CreateAsync(_communityFollowProvider, Community);
                    }
                }
                catch
                {
                    FollowContext = CommunityFollowContext.Default;
                }

                UpdateCanNotFollowReason();
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


        private RelayCommand _OpenCommunityWebPagePageCommand;
        public RelayCommand OpenCommunityWebPagePageCommand
        {
            get
            {
                return _OpenCommunityWebPagePageCommand
                    ?? (_OpenCommunityWebPagePageCommand = new RelayCommand(async () =>
                    {
                        if (ContentIdHelper.IsCommunityId(CommunityId))
                        {
                            await Launcher.LaunchUriAsync(new Uri(NiconicoUrls.MakeCommunityPageUrl(CommunityId)));
                        }
                    }));
            }
        }

        private RelayCommand _OpenCommunityVideoListPageCommand;
		public RelayCommand OpenCommunityVideoListPageCommand
		{
			get
			{
				return _OpenCommunityVideoListPageCommand
					?? (_OpenCommunityVideoListPageCommand = new RelayCommand(() =>
					{
						PageManager.OpenPageWithId(HohoemaPageType.CommunityVideo, CommunityId);
					}));
			}
		}

		private RelayCommand<Uri> _ScriptNotifyCommand;
        public RelayCommand<Uri> ScriptNotifyCommand
		{
			get
			{
				return _ScriptNotifyCommand
					?? (_ScriptNotifyCommand = new RelayCommand<Uri>((parameter) =>
					{
						System.Diagnostics.Debug.WriteLine($"script notified: {parameter}");

						PageManager.OpenPage(parameter);
					}));
			}
		}

        private void UpdateCanNotFollowReason()
		{
			if (FollowContext.IsFollowing)
			{
				CanNotFollowReason = null;
			}
			else
			{
				if (!CommunityInfo.IsPublic)
				{
					CanNotFollowReason = "参加には承認が必要";
				}
				//else if (CommunityInfo.OptionFlagDetails.CommunityPrivUserAuth == "1")
				//{
				//	CanNotFollowReason = "参加には個人情報公開が必要";
				//}
				else
				{
					CanNotFollowReason = null;
				}
			}

			OnPropertyChanged(nameof(CanNotFollowReason));
		}
    }
}
