using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models;
using Reactive.Bindings;
using Prism.Commands;
using Hohoema.Models.Helpers;
using Windows.ApplicationModel.DataTransfer;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.System;
using Hohoema.Interfaces;
using Prism.Navigation;
using Hohoema.ViewModels.Pages;
using Hohoema.UseCase.Playlist;
using Hohoema.UseCase.Playlist.Commands;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Hohoema.UseCase;
using Windows.UI.Xaml;
using Hohoema.ViewModels.Subscriptions;
using Hohoema.Models.Pages;
using Hohoema.Models.Repository.App;
using Hohoema.Models.Repository.Niconico.NicoVideo;
using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.UseCase.VideoCache;
using Hohoema.Models.Subscriptions;
using Hohoema.Models.Niconico;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Repository.Niconico.Ichiba;

namespace Hohoema.ViewModels
{
    public class VideoInfomationPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
    {
        Models.Pages.HohoemaPin IPinablePage.GetPin()
        {
            return new Models.Pages.HohoemaPin()
            {
                Label = VideoDetals.VideoTitle,
                PageType = HohoemaPageType.VideoInfomation,
                Parameter = $"id={VideoInfo.VideoId}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.VideoDetals).Select(x => x?.VideoTitle);
        }

        public VideoInfomationPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            AppearanceSettingsRepository appearanceSettings,
            VideoListFilterSettings videoListFilterSettings,
            NiconicoSession niconicoSession,
            UserMylistManager userMylistManager,
            HohoemaPlaylist hohoemaPlaylist,
            NicoVideoProvider nicoVideoProvider,
            NicoIchibaRepository nicoIchibaRepository,
            LoginUserMylistProvider loginUserMylistProvider,
            VideoCacheManager videoCacheManager,
            SubscriptionManager subscriptionManager,
            NicoVideoSessionProvider nicoVideo,
            PageManager pageManager,
            AddMylistCommand addMylistCommand,
            AddSubscriptionCommand addSubscriptionCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            _appearanceSettings = appearanceSettings;
            _videoListFilterSettings = videoListFilterSettings;
            NiconicoSession = niconicoSession;
            UserMylistManager = userMylistManager;
            HohoemaPlaylist = hohoemaPlaylist;
            NicoVideoProvider = nicoVideoProvider;
            _nicoIchibaRepository = nicoIchibaRepository;
            LoginUserMylistProvider = loginUserMylistProvider;
            VideoCacheManager = videoCacheManager;
            SubscriptionManager = subscriptionManager;
            NicoVideo = nicoVideo;
            PageManager = pageManager;
            AddMylistCommand = addMylistCommand;
            AddSubscriptionCommand = addSubscriptionCommand;
            NowLoading = new ReactiveProperty<bool>(false);
            IsLoadFailed = new ReactiveProperty<bool>(false);
        }

        public Database.NicoVideo VideoInfo { get; private set; }

        public NicoVideoSessionProvider NicoVideo { get; private set; }

        public ReactiveProperty<bool> NowLoading { get; private set; }
        public ReactiveProperty<bool> IsLoadFailed { get; private set; }

        public IReadOnlyList<IchibaItem> IchibaItems { get; private set; }

        public bool IsSelfZoningContent { get; private set; }
        public Models.Repository.App.NGResult SelfZoningInfo { get; private set; }


        public Uri DescriptionHtmlFileUri { get; private set; }


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
                        if (VideoInfo.Owner.UserType == Database.NicoVideoUserType.User)
                        {
                            PageManager.OpenPageWithId(HohoemaPageType.UserInfo, VideoInfo.Owner.OwnerId);
                        }
                    }
                    , () => VideoInfo?.Owner.UserType == Database.NicoVideoUserType.User
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
                        if (VideoInfo.Owner.UserType == Database.NicoVideoUserType.User)
                        {
                            PageManager.OpenPageWithId(HohoemaPageType.UserVideo, VideoInfo.Owner.OwnerId);
                        }
                        else if (VideoDetals.IsChannelOwnedVideo)
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
                        ShareHelper.Share(VideoInfo);
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
                        ClipboardHelper.CopyToClipboard(VideoInfo);
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
                        if (this.VideoInfo?.Owner?.UserType == Database.NicoVideoUserType.User)
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
                        if (this.VideoDetals.Series != null)
                        {
                            PageManager.OpenPageWithId(HohoemaPageType.Series, this.VideoDetals.Series.Id.ToString());
                        }
                    }));
            }
        }

        Regex GeneralUrlRegex = new Regex(@"https?:\/\/([a-zA-Z0-9.\/?=_-]*)");
        public List<HyperlinkItem> VideoDescriptionHyperlinkItems { get; } = new List<HyperlinkItem>();
       

        Models.Helpers.AsyncLock _UpdateLock = new AsyncLock();

        public List<VideoInfoControlViewModel> RelatedVideos { get; private set; }


        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public NiconicoSession NiconicoSession { get; }
        public UserMylistManager UserMylistManager { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public LoginUserMylistProvider LoginUserMylistProvider { get; }
        public VideoCacheManager VideoCacheManager { get; }
        public SubscriptionManager SubscriptionManager { get; }
        public PageManager PageManager { get; }
        public AddMylistCommand AddMylistCommand { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }

        public INicoVideoDetails VideoDetals { get; private set; }

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
            IchibaItems = null;
            RaisePropertyChanged(nameof(IchibaItems));

            base.OnNavigatedFrom(parameters);
        }

        bool _IsInitializedIchibaItems = false;
        public async void InitializeIchibaItems()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (_IsInitializedIchibaItems) { return; }

                var ichiba = await _nicoIchibaRepository.GetIchibaAsync(VideoInfo.RawVideoId);
                IchibaItems = ichiba.MainItems;

                RaisePropertyChanged(nameof(IchibaItems));

                _IsInitializedIchibaItems = true;
            }
        }



        bool _IsInitializedRelatedVideos = false;
        private readonly AppearanceSettingsRepository _appearanceSettings;
        private readonly VideoListFilterSettings _videoListFilterSettings;
        private readonly NicoIchibaRepository _nicoIchibaRepository;

        public async void InitializeRelatedVideos()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (_IsInitializedRelatedVideos) { return; }

                var items = await NicoVideoProvider.GetRelatedVideosAsync(VideoInfo.RawVideoId, 0, 10, Sort.Relation);
                
                RelatedVideos = items.VideoItems?.Select(x =>
                {
                    var video = Database.NicoVideoDb.Get(x.Video.Id);
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
                    VideoDetals = res.GetVideoDetails();

                    
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

                DescriptionHtmlFileUri = await Models.Helpers.HtmlFileHelper.PartHtmlOutputToCompletlyHtml(VideoInfo.VideoId, VideoDetals.DescriptionHtml, appTheme);
                RaisePropertyChanged(nameof(DescriptionHtmlFileUri));
            }
            catch
            {
                IsLoadFailed.Value = true;
                return;
            }


            VideoDescriptionHyperlinkItems.Clear();
            try
            {
                var htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(VideoDetals.DescriptionHtml);
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

                var matches = GeneralUrlRegex.Matches(VideoDetals.DescriptionHtml);
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
                    if (_videoListFilterSettings.IsNgVideo(VideoInfo, out var result))
                    {
                        SelfZoningInfo = result;
                    }
                    else
                    {
                        SelfZoningInfo = null;
                    }

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
