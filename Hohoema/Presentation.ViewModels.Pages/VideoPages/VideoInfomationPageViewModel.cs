using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Domain;
using Reactive.Bindings;
using Prism.Commands;
using Hohoema.Models.Domain.Helpers;
using Windows.ApplicationModel.DataTransfer;
using System.Threading;
using System.Diagnostics;
using Mntone.Nico2;
using Mntone.Nico2.Embed.Ichiba;
using System.Text.RegularExpressions;
using Windows.System;
using Hohoema.Models.Domain.Player.Video.Cache;
using Prism.Navigation;
using Hohoema.Presentation.Services.Page;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.NicoVideos;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Hohoema.Models.UseCase;
using Windows.UI.Xaml;
using Hohoema.Models.Domain.PageNavigation;
using NiconicoSession = Hohoema.Models.Domain.NiconicoSession;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Models.Domain.Application;
using Hohoema.Presentation.ViewModels.NicoVideos.Commands;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Hohoema.Presentation.ViewModels.Subscriptions.Commands;
using Hohoema.Presentation.Services;
using I18NPortable;

namespace Hohoema.Presentation.ViewModels.Pages.VideoPages
{
    public class VideoInfomationPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
    {
        HohoemaPin IPinablePage.GetPin()
        {
            return new HohoemaPin()
            {
                Label = VideoDetails.VideoTitle,
                PageType = HohoemaPageType.VideoInfomation,
                Parameter = $"id={VideoInfo.VideoId}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.VideoDetails).Select(x => x?.VideoTitle);
        }

        public VideoInfomationPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            AppearanceSettings appearanceSettings,
            VideoFilteringSettings ngSettings,
            NiconicoSession niconicoSession,
            UserMylistManager userMylistManager,
            HohoemaPlaylist hohoemaPlaylist,
            NicoVideoProvider nicoVideoProvider,
            LoginUserMylistProvider loginUserMylistProvider,
            VideoCacheManager videoCacheManager,
            SubscriptionManager subscriptionManager,
            NicoVideoSessionProvider nicoVideo,
            NicoVideoCacheRepository nicoVideoRepository,
            PageManager pageManager,
            Services.NotificationService notificationService,
            Services.DialogService dialogService,
            Services.ExternalAccessService externalAccessService,
            MylistAddItemCommand addMylistCommand,
            LocalPlaylistAddItemCommand localPlaylistAddItemCommand,
            AddSubscriptionCommand addSubscriptionCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            _appearanceSettings = appearanceSettings;
            NgSettings = ngSettings;
            NiconicoSession = niconicoSession;
            UserMylistManager = userMylistManager;
            HohoemaPlaylist = hohoemaPlaylist;
            NicoVideoProvider = nicoVideoProvider;
            LoginUserMylistProvider = loginUserMylistProvider;
            VideoCacheManager = videoCacheManager;
            SubscriptionManager = subscriptionManager;
            NicoVideo = nicoVideo;
            _nicoVideoRepository = nicoVideoRepository;
            PageManager = pageManager;
            NotificationService = notificationService;
            DialogService = dialogService;
            ExternalAccessService = externalAccessService;
            AddMylistCommand = addMylistCommand;
            LocalPlaylistAddItemCommand = localPlaylistAddItemCommand;
            AddSubscriptionCommand = addSubscriptionCommand;
            NowLoading = new ReactiveProperty<bool>(false);
            IsLoadFailed = new ReactiveProperty<bool>(false);
        }


        private NicoVideo _VideoInfo;
        public NicoVideo VideoInfo
        {
            get { return _VideoInfo; }
            set { SetProperty(ref _VideoInfo, value); }
        }

        public NicoVideoSessionProvider NicoVideo { get; private set; }

        public ReactiveProperty<bool> NowLoading { get; private set; }
        public ReactiveProperty<bool> IsLoadFailed { get; private set; }

        private List<IchibaItem> _ichibaItems;
        public List<IchibaItem> IchibaItems
        {
            get { return _ichibaItems; }
            set { SetProperty(ref _ichibaItems, value); }
        }


        private bool _isSelfZoningContent;
        public bool IsSelfZoningContent
        {
            get { return _isSelfZoningContent; }
            set { SetProperty(ref _isSelfZoningContent, value); }
        }

        private FilteredResult _selfZoningInfo;
        public FilteredResult SelfZoningInfo
        {
            get { return _selfZoningInfo; }
            set { SetProperty(ref _selfZoningInfo, value); }
        }


        private Uri _descriptionHtmlFileUri;
        public Uri DescriptionHtmlFileUri
        {
            get { return _descriptionHtmlFileUri; }
            set { SetProperty(ref _descriptionHtmlFileUri, value); }
        }



        // ニコニコの「いいね」

        private bool _isLikedVideo;
        public bool IsLikedVideo
        {
            get { return _isLikedVideo; }
            set { SetProperty(ref _isLikedVideo, value); }
        }

        private string _LikeThanksMessage;
        public string LikeThanksMessage
        {
            get { return _LikeThanksMessage; }
            private set { SetProperty(ref _LikeThanksMessage, value); }
        }


        private bool _NowLikeProcessing;
        public bool NowLikeProcessing
        {
            get { return _NowLikeProcessing; }
            private set { SetProperty(ref _NowLikeProcessing, value); }
        }



        private DelegateCommand _OpenFilterSettingPageCommand;
        public DelegateCommand OpenFilterSettingPageCommand
        {
            get
            {
                return _OpenFilterSettingPageCommand
                    ?? (_OpenFilterSettingPageCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPage(HohoemaPageType.Settings);
                    }
                    ));
            }
        }


        private DelegateCommand _OpenOwnerUserPageCommand;
        public DelegateCommand OpenOwnerUserPageCommand
        {
            get
            {
                return _OpenOwnerUserPageCommand
                    ?? (_OpenOwnerUserPageCommand = new DelegateCommand(() =>
                    {
                        if (VideoInfo.Owner.UserType == NicoVideoUserType.User)
                        {
                            PageManager.OpenPageWithId(HohoemaPageType.UserInfo, VideoInfo.Owner.OwnerId);
                        }
                    }
                    , () => VideoInfo?.Owner.UserType == NicoVideoUserType.User
                    ));
            }
        }


        private DelegateCommand _OpenOwnerUserVideoPageCommand;
        public DelegateCommand OpenOwnerUserVideoPageCommand
        {
            get
            {
                return _OpenOwnerUserVideoPageCommand
                    ?? (_OpenOwnerUserVideoPageCommand = new DelegateCommand(() =>
                    {
                        if (VideoInfo.Owner.UserType == NicoVideoUserType.User)
                        {
                            PageManager.OpenPageWithId(HohoemaPageType.UserVideo, VideoInfo.Owner.OwnerId);
                        }
                        else if (VideoDetails.IsChannelOwnedVideo)
                        {
                            PageManager.OpenPageWithId(HohoemaPageType.ChannelVideo, VideoInfo.Owner.OwnerId);
                        }
                    }
                    ));
            }
        }


        private DelegateCommand _PlayVideoCommand;
        public DelegateCommand PlayVideoCommand
        {
            get
            {
                return _PlayVideoCommand
                    ?? (_PlayVideoCommand = new DelegateCommand(() =>
                    {
                        HohoemaPlaylist.Play(VideoInfo);
                    }
                    ));
            }
        }

        private DelegateCommand _ShareCommand;
        public DelegateCommand ShareCommand
        {
            get
            {
                return _ShareCommand
                    ?? (_ShareCommand = new DelegateCommand(() =>
                    {
                        Services.Helpers.ShareHelper.Share(VideoInfo);
                    }
                    , () => DataTransferManager.IsSupported()
                    ));
            }
        }

        private DelegateCommand _VideoInfoCopyToClipboardCommand;
        public DelegateCommand VideoInfoCopyToClipboardCommand
        {
            get
            {
                return _VideoInfoCopyToClipboardCommand
                    ?? (_VideoInfoCopyToClipboardCommand = new DelegateCommand(() =>
                    {
                        Services.Helpers.ClipboardHelper.CopyToClipboard(VideoInfo);
                    }
                    ));
            }
        }

        private DelegateCommand<object> _ScriptNotifyCommand;
        public DelegateCommand<object> ScriptNotifyCommand
        {
            get
            {
                return _ScriptNotifyCommand
                    ?? (_ScriptNotifyCommand = new DelegateCommand<object>(async (parameter) =>
                    {
                        Uri url = parameter as Uri ?? (parameter as HyperlinkItem)?.Url;
                        if (url != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"script notified: {url}");

                            if (false == PageManager.OpenPage(url))
                            {
                                await Launcher.LaunchUriAsync(url);
                            }
                        }
                    }));
            }
        }


        private DelegateCommand<IPlaylist> _AddPlaylistCommand;
        public DelegateCommand<IPlaylist> AddPlaylistCommand
        {
            get
            {
                return _AddPlaylistCommand
                    ?? (_AddPlaylistCommand = new DelegateCommand<IPlaylist>(async (playlist) =>
                    {
                        if (playlist is LocalPlaylist localPlaylist)
                        {
                            localPlaylist.AddPlaylistItem(VideoInfo);
                        }
                        else if (playlist is LoginUserMylistPlaylist loginUserMylist)
                        {
                            _ = loginUserMylist.AddItem(VideoInfo.RawVideoId);
                        }
                    }));
            }
        }


        private DelegateCommand _UpdateCommand;
        public DelegateCommand UpdateCommand
        {
            get
            {
                return _UpdateCommand
                    ?? (_UpdateCommand = new DelegateCommand(async () =>
                    {
                        await UpdateVideoDescription();
                    }));
            }
        }


        private DelegateCommand _OpenUserSeriesPageCommand;
        public DelegateCommand OpenUserSeriesPageCommand
        {
            get
            {
                return _OpenUserSeriesPageCommand
                    ?? (_OpenUserSeriesPageCommand = new DelegateCommand(() =>
                    {
                        if (this.VideoInfo?.Owner?.UserType == NicoVideoUserType.User)
                        {
                            PageManager.OpenPageWithId(HohoemaPageType.UserSeries, this.VideoInfo.Owner.OwnerId);
                        }
                    }));
            }
        }


        private DelegateCommand _OpenVideoBelongSeriesPageCommand;
        public DelegateCommand OpenVideoBelongSeriesPageCommand
        {
            get
            {
                return _OpenVideoBelongSeriesPageCommand
                    ?? (_OpenVideoBelongSeriesPageCommand = new DelegateCommand(() =>
                    {
                        if (this.VideoDetails.Series != null)
                        {
                            PageManager.OpenPageWithId(HohoemaPageType.Series, this.VideoDetails.Series.Id.ToString());
                        }
                    }));
            }
        }

        Regex GeneralUrlRegex = new Regex(@"https?:\/\/([a-zA-Z0-9.\/?=_-]*)");

        private List<HyperlinkItem> _VideoDescriptionHyperlinkItems;
        public List<HyperlinkItem> VideoDescriptionHyperlinkItems
        {
            get { return _VideoDescriptionHyperlinkItems; }
            set { SetProperty(ref _VideoDescriptionHyperlinkItems, value); }
        }       

        Models.Domain.Helpers.AsyncLock _UpdateLock = new AsyncLock();

        private List<VideoInfoControlViewModel> _relatedVideos;
        public List<VideoInfoControlViewModel> RelatedVideos
        {
            get { return _relatedVideos; }
            set { SetProperty(ref _relatedVideos, value); }
        }


        public Services.NotificationService NotificationService { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public VideoFilteringSettings NgSettings { get; }
        public NiconicoSession NiconicoSession { get; }
        public UserMylistManager UserMylistManager { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public LoginUserMylistProvider LoginUserMylistProvider { get; }
        public VideoCacheManager VideoCacheManager { get; }
        public SubscriptionManager SubscriptionManager { get; }
        public PageManager PageManager { get; }
        public Services.DialogService DialogService { get; }
        public Services.ExternalAccessService ExternalAccessService { get; }
        public MylistAddItemCommand AddMylistCommand { get; }
        public LocalPlaylistAddItemCommand LocalPlaylistAddItemCommand { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }

        private INicoVideoDetails _VideoDetails;
        public INicoVideoDetails VideoDetails
        {
            get { return _VideoDetails; }
            set { SetProperty(ref _VideoDetails, value); }
        }

        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            NowLoading.Value = true;
            IsLoadFailed.Value = false;

            try
            {
                if (parameters.TryGetValue("id", out string videoId))
                {
                    
                    if (videoId == null)
                    {
                        IsLoadFailed.Value = true;
                        throw new Exception();
                    }

                    VideoInfo = await NicoVideoProvider.GetNicoVideoInfo(videoId);
                    RaisePropertyChanged(nameof(VideoInfo));

                    await UpdateVideoDescription();

                    UpdateSelfZoning();

                    OpenOwnerUserPageCommand.RaiseCanExecuteChanged();
                    OpenOwnerUserVideoPageCommand.RaiseCanExecuteChanged();

                    // 好きの切り替え
                    this.ObserveProperty(x => x.IsLikedVideo)
                        .Where(x => !NowLikeProcessing)
                        .Subscribe(async like =>
                        {
                            await ProcessLikeAsync(like);
                        })
                        .AddTo(_NavigatingCompositeDisposable);
                }                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                NowLoading.Value = false;
            }

        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            VideoDescriptionHyperlinkItems?.Clear();
            RaisePropertyChanged(nameof(VideoDescriptionHyperlinkItems));
            IchibaItems?.Clear();
            RaisePropertyChanged(nameof(IchibaItems));

            _IsInitializedIchibaItems = false;
            _IsInitializedRelatedVideos = false;

            base.OnNavigatedFrom(parameters);
        }



        private async Task ProcessLikeAsync(bool like)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            NowLikeProcessing = true;

            try
            {
                if (like)
                {
                    var res = await NiconicoSession.Context.User.DoLikeVideoAsync(this.VideoInfo.VideoId);
                    if (!res.IsOK)
                    {
                        this.IsLikedVideo = false;
                    }
                    else
                    {
                        LikeThanksMessage = res.ThanksMessage;

                        if (!string.IsNullOrEmpty(LikeThanksMessage))
                        {
                            NotificationService.ShowInAppNotification(new InAppNotificationPayload()
                            {
                                Title = "LikeThanksMessageDescWithVideoOwnerName".Translate(VideoInfo.Owner?.ScreenName),
                                Content = LikeThanksMessage,
                                IsShowDismissButton = true,
                                ShowDuration = TimeSpan.FromSeconds(7),
                            });
                        }
                    }
                }
                else
                {
                    LikeThanksMessage = null;

                    var res = await NiconicoSession.Context.User.UnDoLikeVideoAsync(this.VideoInfo.VideoId);
                    if (!res.IsOK)
                    {
                        this.IsLikedVideo = true;
                    }
                }
            }
            finally
            {
                NowLikeProcessing = false;
            }
        }


        bool _IsInitializedIchibaItems = false;
        public async void InitializeIchibaItems()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (_IsInitializedIchibaItems) { return; }

                var ichiba = await NiconicoSession.Context.Embed.GetIchiba(VideoInfo.RawVideoId);
                IchibaItems = ichiba.GetMainIchibaItems();

                RaisePropertyChanged(nameof(IchibaItems));

                _IsInitializedIchibaItems = true;
            }
        }



        bool _IsInitializedRelatedVideos = false;
        private readonly AppearanceSettings _appearanceSettings;
        private readonly NicoVideoCacheRepository _nicoVideoRepository;

        public async void InitializeRelatedVideos()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (_IsInitializedRelatedVideos) { return; }

                var items = await NiconicoSession.Context.Video.GetRelatedVideoAsync(VideoInfo.RawVideoId, 0, 10, Sort.Relation);
                
                RelatedVideos = items.Video_info?.Select(x =>
                {
                    var video = _nicoVideoRepository.Get(x.Video.Id);
                    video.Title = x.Video.Title;
                    video.ThumbnailUrl = x.Video.Thumbnail_url;

                    var vm = new VideoInfoControlViewModel(video);
                    return vm;
                })
                .ToList();

                RaisePropertyChanged(nameof(RelatedVideos));

                _IsInitializedRelatedVideos = true;
            }
        }
        private async Task UpdateVideoDescription()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (VideoInfo.RawVideoId == null)
                {
                    return;
                }

                IsLoadFailed.Value = false;

                try
                {
                    var res = await NicoVideo.PreparePlayVideoAsync(VideoInfo.RawVideoId);
                    VideoDetails = res.GetVideoDetails();


                    //VideoTitle = details.VideoTitle;
                    //Tags = details.Tags.ToList();
                    //ThumbnailUrl = details.ThumbnailUrl;
                    //VideoLength = details.VideoLength;
                    //SubmitDate = details.SubmitDate;
                    //ViewCount = details.ViewCount;
                    //CommentCount = details.CommentCount;
                    //MylistCount = details.MylistCount;
                    //ProviderId = details.ProviderId;
                    //ProviderName = details.ProviderName;
                    //OwnerIconUrl = details.OwnerIconUrl;
                    //IsChannelOwnedVideo = details.IsChannelOwnedVideo;

                    NowLikeProcessing = true;
                    IsLikedVideo = VideoDetails.IsLikedVideo;
                    NowLikeProcessing = false;
                }
                catch
                {
                    IsLoadFailed.Value = true;
                    return;
                }
            }

            try
            {
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

                DescriptionHtmlFileUri = await HtmlFileHelper.PartHtmlOutputToCompletlyHtml(VideoInfo.VideoId, VideoDetails.DescriptionHtml, appTheme);
                RaisePropertyChanged(nameof(DescriptionHtmlFileUri));
            }
            catch
            {
                IsLoadFailed.Value = true;
                return;
            }


            VideoDescriptionHyperlinkItems ??= new List<HyperlinkItem>();
            VideoDescriptionHyperlinkItems.Clear();
            try
            {
                var htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(VideoDetails.DescriptionHtml);
                var root = htmlDocument.DocumentNode;
                var anchorNodes = root.Descendants("a");

                foreach (var anchor in anchorNodes)
                {
                    var href = anchor.Attributes["href"].Value;
                    if (!Uri.IsWellFormedUriString(href, UriKind.Absolute))
                    {
                        Debug.WriteLine("リンク抽出スキップ: " + anchor.InnerText);
                        continue;
                    }

                    VideoDescriptionHyperlinkItems.Add(new HyperlinkItem()
                    {
                        Label = anchor.InnerText,
                        Url = new Uri(href)
                    });

                    Debug.WriteLine($"{anchor.InnerText} : {anchor.Attributes["href"].Value}");
                }

                var matches = GeneralUrlRegex.Matches(VideoDetails.DescriptionHtml);
                foreach (var match in matches.Cast<Match>())
                {
                    if (!VideoDescriptionHyperlinkItems.Any(x => x.Url.OriginalString == match.Value))
                    {
                        VideoDescriptionHyperlinkItems.Add(new HyperlinkItem()
                        {
                            Label = match.Value,
                            Url = new Uri(match.Value)
                        });

                        Debug.WriteLine($"{match.Value} : {match.Value}");
                    }
                }

                RaisePropertyChanged(nameof(VideoDescriptionHyperlinkItems));

            }
            catch
            {
                Debug.WriteLine("動画説明からリンクを抜き出す処理に失敗");
            }

            
        }

        private void UpdateSelfZoning()
        {
            try
            {
                if (VideoInfo != null)
                {
                    
                    NgSettings.TryGetHiddenReason(VideoInfo, out var result);
                    SelfZoningInfo = result;
                    IsSelfZoningContent = SelfZoningInfo != null;

                    RaisePropertyChanged(nameof(SelfZoningInfo));
                    RaisePropertyChanged(nameof(IsSelfZoningContent));
                }
            }
            catch
            {
                IsLoadFailed.Value = true;
                return;
            }
        }
    }


    public class HyperlinkItem
    {
        public string Label { get; set; }
        public Uri Url { get; set; }
    }
}
