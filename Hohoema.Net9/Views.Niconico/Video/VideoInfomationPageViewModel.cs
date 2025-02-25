﻿#nullable enable
using AngleSharp.Html.Parser;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Helpers;
using Hohoema.Models.Application;
using Hohoema.Models.LocalMylist;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.Mylist.LoginUser;
using Hohoema.Models.Niconico.Recommend;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.Series;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Models.Player.Video;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using Hohoema.Services;
using Hohoema.Services.LocalMylist;
using Hohoema.Services.Niconico;
using Hohoema.ViewModels.Navigation.Commands;
using Hohoema.ViewModels.Niconico.Follow;
using Hohoema.ViewModels.Niconico.Likes;
using Hohoema.ViewModels.Niconico.Share;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.Pages.VideoListPage.Commands;
using Hohoema.ViewModels.Subscriptions;
using Hohoema.ViewModels.VideoCache.Commands;
using Hohoema.ViewModels.VideoListPage;
using Microsoft.Extensions.Logging;
using NiconicoToolkit.Ichiba;
using NiconicoToolkit.Video;
using NiconicoToolkit.Video.Watch;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using ZLogger;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;

namespace Hohoema.ViewModels.Pages.Niconico.Video;

public class VideoInfomationPageViewModel : HohoemaPageViewModelBase, IPinablePage, ITitleUpdatablePage
{
    HohoemaPin IPinablePage.GetPin()
    {
        return new HohoemaPin()
        {
            Label = VideoDetails.Title,
            PageType = HohoemaPageType.VideoInfomation,
            Parameter = $"id={VideoInfo.VideoAliasId}"
        };
    }

    IObservable<string> ITitleUpdatablePage.GetTitleObservable()
    {
        return this.ObserveProperty(x => x.VideoDetails).Select(x => x?.Title);
    }

    public VideoInfomationPageViewModel(
        IMessenger messenger,
        ILoggerFactory loggerFactory, 
        ApplicationLayoutManager applicationLayoutManager,
        AppearanceSettings appearanceSettings,
        VideoFilteringSettings ngSettings,
        NiconicoSession niconicoSession,
        LoginUserOwnedMylistManager userMylistManager,
        NicoVideoProvider nicoVideoProvider,
        LoginUserMylistProvider loginUserMylistProvider,
        SubscriptionManager subscriptionManager,
        NicoVideoSessionProvider nicoVideo,
        Services.NotificationService notificationService,
        OpenPageCommand openPageCommand,
        OpenContentOwnerPageCommand openContentOwnerPageCommand,
        OpenVideoListPageCommand openVideoListPageCommand,
        VideoPlayWithQueueCommand videoPlayWithQueueCommand,
        MylistAddItemCommand addMylistCommand,
        LocalPlaylistAddItemCommand localPlaylistAddItemCommand,
        AddSubscriptionCommand addSubscriptionCommand,
        OpenLinkCommand openLinkCommand,
        CopyToClipboardCommand copyToClipboardCommand,
        CopyToClipboardWithShareTextCommand copyToClipboardWithShareTextCommand,
        OpenShareUICommand openShareUICommand,
        CacheAddRequestCommand cacheAddRequestCommand,
        RecommendProvider recommendProvider,
        UserFollowProvider userFollowProvider,
        ChannelFollowProvider channelFollowProvider
        )
    {
        _logger = loggerFactory.CreateLogger<VideoInfomationPageViewModel>();
        _messenger = messenger;
        ApplicationLayoutManager = applicationLayoutManager;
        AppearanceSettings = appearanceSettings;
        NgSettings = ngSettings;
        NiconicoSession = niconicoSession;
        UserMylistManager = userMylistManager;
        NicoVideoProvider = nicoVideoProvider;
        LoginUserMylistProvider = loginUserMylistProvider;
        SubscriptionManager = subscriptionManager;
        NicoVideo = nicoVideo;        
        NotificationService = notificationService;
        OpenPageCommand = openPageCommand;
        OpenContentOwnerPageCommand = openContentOwnerPageCommand;
        OpenVideoListPageCommand = openVideoListPageCommand;
        VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
        AddMylistCommand = addMylistCommand;
        LocalPlaylistAddItemCommand = localPlaylistAddItemCommand;
        AddSubscriptionCommand = addSubscriptionCommand;
        OpenLinkCommand = openLinkCommand;
        CopyToClipboardCommand = copyToClipboardCommand;
        CopyToClipboardWithShareTextCommand = copyToClipboardWithShareTextCommand;
        OpenShareUICommand = openShareUICommand;
        CacheAddRequestCommand = cacheAddRequestCommand;
        _recommendProvider = recommendProvider;
        _userFollowProvider = userFollowProvider;
        _channelFollowProvider = channelFollowProvider;
        NowLoading = new ReactiveProperty<bool>(false);
        IsLoadFailed = new ReactiveProperty<bool>(false);


    }

    private IFollowContext _FollowContext;
    public IFollowContext FollowContext
    {
        get => _FollowContext;
        set => SetProperty(ref _FollowContext, value);
    }


    private NicoVideo _VideoInfo;
    public NicoVideo VideoInfo
    {
        get { return _VideoInfo; }
        set { SetProperty(ref _VideoInfo, value); }
    }

    private VideoSeriesViewModel _VideoSeries;
    public VideoSeriesViewModel VideoSeries
    {
        get { return _VideoSeries; }
        set { SetProperty(ref _VideoSeries, value); }
    }

    private VideoListItemControlViewModel[] _prevSeriesVideo;
    public VideoListItemControlViewModel[] PrevSeriesVideo
    {
        get => _prevSeriesVideo;
        set => SetProperty(ref _prevSeriesVideo, value);
    }


    private VideoListItemControlViewModel[] _nextSeriesVideo;
    public VideoListItemControlViewModel[] NextSeriesVideo
    {
        get => _nextSeriesVideo;
        set => SetProperty(ref _nextSeriesVideo, value);
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


    private string _descriptionHtml;
    public string DescriptionHtml
    {
        get { return _descriptionHtml; }
        set { SetProperty(ref _descriptionHtml, value); }
    }



    // ニコニコの「いいね」
    private VideoLikesContext _LikesContext = VideoLikesContext.Default;
    public VideoLikesContext LikesContext
    {
        get => _LikesContext;
        set => SetProperty(ref _LikesContext, value);
    }


    private RelayCommand _OpenFilterSettingPageCommand;
    public RelayCommand OpenFilterSettingPageCommand
    {
        get
        {
            return _OpenFilterSettingPageCommand
                ?? (_OpenFilterSettingPageCommand = new RelayCommand(() =>
                {
                    _ = _messenger.OpenPageAsync(HohoemaPageType.Settings);
                }
                ));
        }
    }


    private RelayCommand _OpenOwnerUserPageCommand;
    public RelayCommand OpenOwnerUserPageCommand
    {
        get
        {
            return _OpenOwnerUserPageCommand
                ?? (_OpenOwnerUserPageCommand = new RelayCommand(() =>
                {
                    if (VideoInfo.Owner.UserType == OwnerType.User)
                    {
                        _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.UserInfo, VideoInfo.Owner.OwnerId);
                    }
                }
                , () => VideoInfo?.Owner.UserType == OwnerType.User
                ));
        }
    }


    private RelayCommand _OpenOwnerUserVideoPageCommand;
    public RelayCommand OpenOwnerUserVideoPageCommand
    {
        get
        {
            return _OpenOwnerUserVideoPageCommand
                ?? (_OpenOwnerUserVideoPageCommand = new RelayCommand(() =>
                {
                    if (VideoInfo.Owner.UserType == OwnerType.User)
                    {
                        _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.UserVideo, VideoInfo.Owner.OwnerId);
                    }
                    else if (VideoDetails.IsChannelOwnedVideo)
                    {
                        _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.ChannelVideo, VideoInfo.Owner.OwnerId);
                    }
                }
                ));
        }
    }


    private RelayCommand _ShareCommand;
    public RelayCommand ShareCommand
    {
        get
        {
            return _ShareCommand
                ?? (_ShareCommand = new RelayCommand(() =>
                {
                    ShareHelper.Share(VideoInfo);
                }
                , () => DataTransferManager.IsSupported()
                ));
        }
    }

    private RelayCommand _VideoInfoCopyToClipboardCommand;
    public RelayCommand VideoInfoCopyToClipboardCommand
    {
        get
        {
            return _VideoInfoCopyToClipboardCommand
                ?? (_VideoInfoCopyToClipboardCommand = new RelayCommand(() =>
                {
                    ClipboardHelper.CopyToClipboard(VideoInfo);
                }
                ));
        }
    }

    private RelayCommand<object> _ScriptNotifyCommand;
    public RelayCommand<object> ScriptNotifyCommand
    {
        get
        {
            return _ScriptNotifyCommand
                ?? (_ScriptNotifyCommand = new RelayCommand<object>(async (parameter) =>
                {
                    Uri? url = parameter as Uri ?? (parameter as HyperlinkItem)?.Url;
                    if (url != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"script notified: {url}");

                        if (!await _messenger.OpenUriAsync(url))
                        {
                            await Launcher.LaunchUriAsync(url);
                        }
                    }
                }));
        }
    }


    private RelayCommand<IPlaylist> _AddPlaylistCommand;
    public RelayCommand<IPlaylist> AddPlaylistCommand
    {
        get
        {
            return _AddPlaylistCommand
                ?? (_AddPlaylistCommand = new RelayCommand<IPlaylist>((playlist) =>
                {
                    if (playlist is LocalPlaylist localPlaylist)
                    {
                        localPlaylist.AddPlaylistItem(VideoInfo);
                    }
                    else if (playlist is LoginUserMylistPlaylist loginUserMylist)
                    {
                        _ = loginUserMylist.AddItem(VideoInfo);
                    }
                }));
        }
    }


    private RelayCommand _UpdateCommand;
    public RelayCommand UpdateCommand
    {
        get
        {
            return _UpdateCommand
                ?? (_UpdateCommand = new RelayCommand(async () =>
                {
                    await UpdateVideoDescription();
                }));
        }
    }


    private RelayCommand _OpenUserSeriesPageCommand;
    public RelayCommand OpenUserSeriesPageCommand
    {
        get
        {
            return _OpenUserSeriesPageCommand
                ?? (_OpenUserSeriesPageCommand = new RelayCommand(() =>
                {
                    if (this.VideoInfo?.Owner?.UserType == OwnerType.User)
                    {
                        _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.UserSeries, this.VideoInfo.Owner.OwnerId);
                    }
                }));
        }
    }


    private RelayCommand _OpenVideoBelongSeriesPageCommand;
    public RelayCommand OpenVideoBelongSeriesPageCommand
    {
        get
        {
            return _OpenVideoBelongSeriesPageCommand
                ?? (_OpenVideoBelongSeriesPageCommand = new RelayCommand(() =>
                {
                    if (this.VideoDetails.Series != null)
                    {
                        _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.Series, this.VideoDetails.Series.Id.ToString());
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

    Helpers.AsyncLock _UpdateLock = new AsyncLock();

    private List<VideoListItemControlViewModel> _relatedVideos;
    public List<VideoListItemControlViewModel> RelatedVideos
    {
        get { return _relatedVideos; }
        set { SetProperty(ref _relatedVideos, value); }
    }


    public Services.NotificationService NotificationService { get; }
    public OpenPageCommand OpenPageCommand { get; }
    public OpenContentOwnerPageCommand OpenContentOwnerPageCommand { get; }
    public OpenVideoListPageCommand OpenVideoListPageCommand { get; }

    private readonly ILogger<VideoInfomationPageViewModel> _logger;

    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public VideoFilteringSettings NgSettings { get; }
    public NiconicoSession NiconicoSession { get; }
    public LoginUserOwnedMylistManager UserMylistManager { get; }
    public LocalMylistManager LocalMylistManager { get; }        
    public NicoVideoProvider NicoVideoProvider { get; }
    public LoginUserMylistProvider LoginUserMylistProvider { get; }
    public SubscriptionManager SubscriptionManager { get; }
    public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
    public MylistAddItemCommand AddMylistCommand { get; }
    public LocalPlaylistAddItemCommand LocalPlaylistAddItemCommand { get; }
    public AddSubscriptionCommand AddSubscriptionCommand { get; }
    public OpenLinkCommand OpenLinkCommand { get; }
    public CopyToClipboardCommand CopyToClipboardCommand { get; }
    public CopyToClipboardWithShareTextCommand CopyToClipboardWithShareTextCommand { get; }
    public OpenShareUICommand OpenShareUICommand { get; }
    public CacheAddRequestCommand CacheAddRequestCommand { get; }

    private INicoVideoDetails _VideoDetails;
    public INicoVideoDetails VideoDetails
    {
        get { return _VideoDetails; }
        set { SetProperty(ref _VideoDetails, value); }
    }


    private bool _isVideoProviderDeleted;
    public bool IsVideoProviderDeleted
    {
        get => _isVideoProviderDeleted;
        private set => SetProperty(ref _isVideoProviderDeleted, value);
    }
    CancellationTokenSource _navigationCts;
    CancellationToken _navigationCancellationToken;
    public override async Task OnNavigatedToAsync(INavigationParameters parameters)
    {
        await base.OnNavigatedToAsync(parameters);

        _navigationCts = new CancellationTokenSource();
        _navigationCancellationToken = _navigationCts.Token;

        NowLoading.Value = true;
        IsLoadFailed.Value = false;

        VideoId? videoId = null;
        if (parameters.TryGetValue("id", out string strVideoId))
        {
            videoId = strVideoId;
        }
        else if (parameters.TryGetValue("id", out VideoId justVideoId))
        {
            videoId = justVideoId;
        }

        if (!videoId.HasValue)
        {
            IsLoadFailed.Value = true;
            NowLoading.Value = false;
            return;
        }


        try
        {

            // 投稿者情報やHTMLなDescriptionが必要なので、オンラインから情報取得
            (_, VideoInfo) = await NicoVideoProvider.GetVideoInfoAsync(videoId.Value);

            await UpdateVideoDescription();

            IsVideoProviderDeleted = string.IsNullOrEmpty(VideoInfo.ProviderName);

            UpdateSelfZoning();

            OpenOwnerUserPageCommand.NotifyCanExecuteChanged();
            OpenOwnerUserVideoPageCommand.NotifyCanExecuteChanged();

            // 好きの切り替え
            if (NiconicoSession.IsLoggedIn && VideoDetails != null)
            {
                LikesContext = new VideoLikesContext(VideoDetails, NiconicoSession.ToolkitContext.Likes, NotificationService);
            }

            if (NiconicoSession.IsLoggedIn)
            {
                try
                {
                    var owner = await NicoVideoProvider.ResolveVideoOwnerAsync(videoId.Value);
                    FollowContext = VideoInfo.ProviderType switch
                    {
                        OwnerType.User => await FollowContext<IUser>.CreateAsync(_userFollowProvider, owner),
                        OwnerType.Channel => await FollowContext<IChannel>.CreateAsync(_channelFollowProvider, owner),
                        _ => FollowContext<IUser>.Default
                    };
                }
                catch (Exception ex)
                {
                    FollowContext = FollowContext<IUser>.Default;
                    _logger.LogError(ex, "FollowContext creation failed.");
                    throw;
                }
            }
        }
        finally
        {
            NowLoading.Value = false;
        }

    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        _navigationCts.Cancel();
        _navigationCts.Dispose();
        _navigationCts = null;

        VideoDescriptionHyperlinkItems?.Clear();
        IchibaItems?.Clear();

        _IsInitializedIchibaItems = false;
        _IsInitializedRelatedVideos = false;
        
        FollowContext = FollowContext<IUser>.Default;
      
        if (NextSeriesVideo != null)
        {
            NextSeriesVideo = null;
        }

        if (PrevSeriesVideo != null)
        {
            PrevSeriesVideo = null;
        }

        // ListViewのメモリリークを抑えるため関連するバインディングをnull埋め
        VideoInfo = null;
        RelatedVideos = null;
        IchibaItems = null;
        VideoDescriptionHyperlinkItems = null;

        base.OnNavigatedFrom(parameters);
    }

    
    bool _IsInitializedIchibaItems = false;
    public async void InitializeIchibaItems()
    {
        if (_IsInitializedIchibaItems) { return; }

        try
        {
            var ichiba = await NiconicoSession.ToolkitContext.Ichiba.GetIchibaItemsAsync(VideoInfo.Id);
            IchibaItems = ichiba.MainItems;
            OnPropertyChanged(nameof(IchibaItems));
        }
        catch (Exception e)
        {
            _logger.ZLogErrorWithPayload(exception: e, VideoInfo.Id, "Video ichiba items loading failed");
        }
        finally
        {
            _IsInitializedIchibaItems = true;
        }
    }



    bool _IsInitializedRelatedVideos = false;
    public AppearanceSettings AppearanceSettings { get; }

    private readonly IMessenger _messenger;
    private readonly RecommendProvider _recommendProvider;
    private readonly UserFollowProvider _userFollowProvider;
    private readonly ChannelFollowProvider _channelFollowProvider;

    public async void  InitializeRelatedVideos()
    {
        if (_IsInitializedRelatedVideos) { return; }

        try
        {
            var res = await _recommendProvider.GetVideoRecommendAsync(VideoInfo.Id);

            if (_navigationCancellationToken.IsCancellationRequested) { return; }

            if (res?.IsSuccess ?? false)
            {
                List<VideoListItemControlViewModel> items = new List<VideoListItemControlViewModel>();

                foreach (var x in res.Data.Items)
                {
                    if (x.ContentType != NiconicoToolkit.Recommend.RecommendContentType.Video)
                    {
                        continue;
                    }

                    var video = x.ContentAsVideo;
                    var vm = new VideoListItemControlViewModel(video);
                    items.Add(vm);
                }

                if (_navigationCancellationToken.IsCancellationRequested)
                {
                    return;
                }

                RelatedVideos = items;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogErrorWithPayload(exception: ex, VideoInfo.Id, "Video recommand video items loading failed");
        }
        finally
        {
            _IsInitializedRelatedVideos = true;
        }            
    }


    private async Task UpdateVideoDescription()
    {
        if (VideoInfo.Id == null)
        {
            return;
        }

        IsLoadFailed.Value = false;

        try
        {
            var res = await NicoVideo.PreparePlayVideoAsync(VideoInfo.Id, noHistory: true);
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

            if (VideoDetails.Series is not null and var series)
            {
                VideoSeries = new VideoSeriesViewModel(series);

                if (series.Video.Next is not null and var nextSeriesVideo)
                {
                    NextSeriesVideo = new[] { new VideoListItemControlViewModel(nextSeriesVideo) };
                }

                if (series.Video.Prev is not null and var prevSeriesVideo)
                {
                    PrevSeriesVideo = new[] { new VideoListItemControlViewModel(prevSeriesVideo) };
                }
            }
        }
        catch
        {
            IsLoadFailed.Value = true;
            throw;
        }
        

        try
        {
            var appTheme = GetCurrentApplicationTheme();

            DescriptionHtml = await HtmlFileHelper.ToCompletlyHtmlAsync(VideoDetails.DescriptionHtml, appTheme);
        }
        catch (Exception e)
        {
            _logger.ZLogErrorWithPayload(exception: e, VideoInfo.Id, "Video info next/prev videos detection failed");
            IsLoadFailed.Value = true;
            throw;
        }


        VideoDescriptionHyperlinkItems ??= new List<HyperlinkItem>();
        VideoDescriptionHyperlinkItems.Clear();
        try
        {
            HtmlParser htmlParser = new HtmlParser();
            using var document = await htmlParser.ParseDocumentAsync(VideoDetails.DescriptionHtml);
            var anchorNodes = document.QuerySelectorAll("a");

            foreach (var anchor in anchorNodes)
            {
                var href = anchor.Attributes["href"].Value;
                if (!Uri.IsWellFormedUriString(href, UriKind.Absolute))
                {
                    Debug.WriteLine("リンク抽出スキップ: " + anchor.TextContent);
                    continue;
                }

                VideoDescriptionHyperlinkItems.Add(new HyperlinkItem()
                {
                    Label = anchor.TextContent,
                    Url = new Uri(href)
                });

                Debug.WriteLine($"{anchor.TextContent} : {anchor.Attributes["href"].Value}");
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
        }
        catch
        {
            throw;
        }
    }


    private ApplicationTheme GetCurrentApplicationTheme()
    {
        ApplicationTheme appTheme;
        if (AppearanceSettings.ApplicationTheme == ElementTheme.Dark)
        {
            appTheme = ApplicationTheme.Dark;
        }
        else if (AppearanceSettings.ApplicationTheme == ElementTheme.Light)
        {
            appTheme = ApplicationTheme.Light;
        }
        else
        {
            appTheme = Views.Helpers.SystemThemeHelper.GetSystemTheme();
        }

        return appTheme;
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

public class VideoSeriesViewModel : ISeries
{
    private readonly WatchApiSeries _userSeries;

    public VideoSeriesViewModel(WatchApiSeries userSeries)
    {
        _userSeries = userSeries;
    }

    public string Id => _userSeries.Id.ToString();

    public string Title => _userSeries.Title;

    public bool IsListed => throw new NotSupportedException();

    public string Description => throw new NotSupportedException();

    public string ThumbnailUrl => _userSeries.ThumbnailUrl.OriginalString;

    public int ItemsCount => throw new NotSupportedException();

    public OwnerType ProviderType => throw new NotSupportedException();

    public string ProviderId => throw new NotSupportedException();
}
