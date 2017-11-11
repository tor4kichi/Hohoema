using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using Prism.Commands;
using NicoPlayerHohoema.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Practices.Unity;
using Prism.Windows.Navigation;
using System.Threading;
using System.Diagnostics;
using Mntone.Nico2;
using Mntone.Nico2.Embed.Ichiba;
using Mntone.Nico2.Videos.WatchAPI;
using Mntone.Nico2.Videos.Dmc;
using NicoPlayerHohoema.Models.Db;

namespace NicoPlayerHohoema.ViewModels
{
    public class VideoInfomationPageViewModel : HohoemaViewModelBase
    {
        Database.NicoVideo _VideoInfo;


        public Uri DescriptionHtmlFileUri { get; private set; }

        public string VideoId { get; private set; }

        public string VideoTitle { get; private set; }

        public string ThumbnailUrl { get; private set; }

        public IList<TagViewModel> Tags { get; private set; }

        public bool IsChannelOwnedVideo { get; private set; }
        public string OwnerName { get; private set; }
        public string OwnerId { get; private set; }
        public string OwnerIconUrl { get; private set; }

        public TimeSpan VideoLength { get; private set; }
        
        public DateTime SubmitDate { get; private set; }

        public uint ViewCount { get; private set; }
        public uint CommentCount { get; private set; }
        public uint MylistCount { get; private set; }

        public Uri VideoPageUri { get; private set; }

        public ReactiveProperty<bool> NowLoading { get; private set; }
        public ReactiveProperty<bool> IsLoadFailed { get; private set; }


        public bool CanDownload { get; private set; }

        public List<IchibaItem> IchibaItems { get; private set; }

        public bool IsSelfZoningContent { get; private set; }
        public NGResult SelfZoningInfo { get; private set; }

        
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
                        if (_VideoInfo.Owner.UserType == Mntone.Nico2.Videos.Thumbnail.UserType.User)
                        {
                            PageManager.OpenPage(HohoemaPageType.UserInfo, _VideoInfo.Owner.OwnerId);
                        }
                    }
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
                        if (_VideoInfo.Owner.UserType == Mntone.Nico2.Videos.Thumbnail.UserType.User)
                        {
                            PageManager.OpenPage(HohoemaPageType.UserVideo, _VideoInfo.Owner.OwnerId);
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
                        HohoemaApp.Playlist.PlayVideo(VideoId, Title);
                    }
                    ));
            }
        }

        private DelegateCommand _CacheRequestCommand;
        public DelegateCommand CacheRequestCommand
        {
            get
            {
                return _CacheRequestCommand
                    ?? (_CacheRequestCommand = new DelegateCommand(() =>
                    {
                        // TODO: 動画情報ページからキャッシュする画質を指定できるようにする
                        HohoemaApp.CacheManager.RequestCache(VideoId, NicoVideoQuality.Smile_Original);
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
                        ShareHelper.Share(_VideoInfo);
                    }
                    , () => DataTransferManager.IsSupported()
                    ));
            }
        }

        private DelegateCommand _ShereWithTwitterCommand;
        public DelegateCommand ShereWithTwitterCommand
        {
            get
            {
                return _ShereWithTwitterCommand
                    ?? (_ShereWithTwitterCommand = new DelegateCommand(async () =>
                    {
                        await ShareHelper.ShareToTwitter(_VideoInfo);
                    }
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
                        ShareHelper.CopyToClipboard(_VideoInfo);
                    }
                    ));
            }
        }

        private DelegateCommand _AddMylistCommand;
        public DelegateCommand AddMylistCommand
        {
            get
            {
                return _AddMylistCommand
                    ?? (_AddMylistCommand = new DelegateCommand(async () =>
                    {
                        var targetMylist = await HohoemaApp.ChoiceMylist() as IPlayableList;

                        if (targetMylist != null)
                        {
                            var result = await HohoemaApp.AddMylistItem(targetMylist, Title, VideoId);
                            (App.Current as App).PublishInAppNotification(
                                InAppNotificationPayload.CreateRegistrationResultNotification(
                                    result,
                                    "マイリスト",
                                    targetMylist.Name,
                                    Title
                                    ));                            
                        }
                    }
                    ));
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


        private DelegateCommand<string> _AddPlaylistCommand;
        public DelegateCommand<string> AddPlaylistCommand
        {
            get
            {
                return _AddPlaylistCommand
                    ?? (_AddPlaylistCommand = new DelegateCommand<string>((playlistId) =>
                    {
                        var hohoemaPlaylist = HohoemaApp.Playlist;
                        var playlist = hohoemaPlaylist.Playlists.FirstOrDefault(x => x.Id == playlistId);

                        if (playlist == null) { return; }

                        playlist.AddVideo(VideoId, Title);
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
                        await Update();
                    }));
            }
        }


        private DelegateCommand<TagViewModel> _OpenTagSearchResultPageCommand;
        public DelegateCommand<TagViewModel> OpenTagSearchResultPageCommand
        {
            get
            {
                return _OpenTagSearchResultPageCommand
                    ?? (_OpenTagSearchResultPageCommand = new DelegateCommand<TagViewModel>((tagVM) =>
                    {
                        tagVM.OpenSearchPageWithTagCommand.Execute();
                    }
                    ));
            }
        }


        public List<LocalMylist> Playlists { get; private set; }


        AsyncLock _WebViewFocusManagementLock = new AsyncLock();
        public VideoInfomationPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
            : base(hohoemaApp, pageManager)
        {
            ChangeRequireServiceLevel(HohoemaAppServiceLevel.OnlineWithoutLoggedIn);

            NowLoading = new ReactiveProperty<bool>(false);
            IsLoadFailed = new ReactiveProperty<bool>(false);
        }


        protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            NowLoading.Value = true;

            if (e.Parameter is string)
            {
                VideoId = e.Parameter as string;
            }

            if (VideoId == null)
            {
                IsLoadFailed.Value = true;
                throw new Exception();
            }

            VideoPageUri = new Uri("http://nicovideo.jp/watch/" + VideoId);
            RaisePropertyChanged(nameof(VideoPageUri));

            Playlists = HohoemaApp.Playlist.Playlists.ToList();

            try
            {
                _VideoInfo = await HohoemaApp.ContentProvider.GetNicoVideoInfo(VideoId);
            }
            catch
            {
                IsLoadFailed.Value = true;

                // サムネ情報取得に失敗

                // 動画情報ページ自体はアクセスできる可能性があるため処理を継続
            }


            try
            {
                await Update();
            }
            catch
            {
                IsLoadFailed.Value = true;
            }

            try
            {
                var ichiba = await HohoemaApp.NiconicoContext.Embed.GetIchiba(VideoId);
                IchibaItems = ichiba.GetMainIchibaItems();
                if (IchibaItems.Count > 0)
                {
                    RaisePropertyChanged(nameof(IchibaItems));
                }
            }
            catch
            {
                Debug.WriteLine(VideoId + " の市場情報の取得に失敗");
            }

            NowLoading.Value = false;
        }


        private async Task Update()
        {
            if (VideoId == null)
            {
                return;
            }

            IsLoadFailed.Value = false;


            CanDownload = HohoemaApp.UserSettings.CacheSettings.IsUserAcceptedCache
                && HohoemaApp.UserSettings.CacheSettings.IsEnableCache
                && HohoemaApp.IsLoggedIn;
            RaisePropertyChanged(nameof(CanDownload));

            var videoDescriptionHtml = string.Empty;
            try
            {

            
            
                var nicoVideo = new NicoVideo(VideoId, HohoemaApp.ContentProvider, HohoemaApp.NiconicoContext, HohoemaApp.CacheManager);

                var res = await nicoVideo.VisitWatchPage(NicoVideoQuality.Dmc_High);

                if (res is WatchApiResponse)
                {
                    var watchApi = res as WatchApiResponse;

                    VideoTitle = watchApi.videoDetail.title;
                    Tags = watchApi.videoDetail.tagList.Select(x => new TagViewModel(x.tag, PageManager))
                        .ToList();
                    ThumbnailUrl = watchApi.videoDetail.thumbnail;
                    VideoLength = TimeSpan.FromSeconds(watchApi.videoDetail.length.Value);
                    SubmitDate = DateTime.Parse(watchApi.videoDetail.postedAt);
                    ViewCount = (uint)watchApi.videoDetail.viewCount.Value;
                    CommentCount = (uint)watchApi.videoDetail.commentCount.Value;
                    MylistCount = (uint)watchApi.videoDetail.mylistCount.Value;
                    OwnerName = watchApi.UserName;
                    OwnerIconUrl = watchApi.UploaderInfo?.icon_url ?? watchApi.channelInfo?.icon_url;
                    IsChannelOwnedVideo = watchApi.channelInfo != null;

                    videoDescriptionHtml = watchApi.videoDetail.description;
                }
                else if (res is DmcWatchData)
                {
                    var dmcWatchApi = (res as DmcWatchData).DmcWatchResponse;

                    VideoTitle = dmcWatchApi.Video.Title;
                    Tags = dmcWatchApi.Tags.Select(x => new TagViewModel(x.Name, PageManager))
                        .ToList();
                    ThumbnailUrl = dmcWatchApi.Video.ThumbnailURL;
                    VideoLength = TimeSpan.FromSeconds(dmcWatchApi.Video.Duration);
                    SubmitDate = DateTime.Parse(dmcWatchApi.Video.PostedDateTime);
                    ViewCount = (uint)dmcWatchApi.Video.ViewCount;
                    CommentCount = (uint)dmcWatchApi.Thread.CommentCount;
                    MylistCount = (uint)dmcWatchApi.Video.MylistCount;
                    OwnerId = dmcWatchApi.Owner?.Nickname ?? dmcWatchApi.Channel?.Name;
                    OwnerName = dmcWatchApi.Owner?.Nickname ?? dmcWatchApi.Channel?.Name;
                    OwnerIconUrl = dmcWatchApi.Owner?.IconURL ?? dmcWatchApi.Channel?.IconURL;
                    IsChannelOwnedVideo = dmcWatchApi.Channel != null;

                    videoDescriptionHtml = dmcWatchApi.Video.Description;
                }
            }
            catch
            {
                IsLoadFailed.Value = true;
                return;
            }



            RaisePropertyChanged(nameof(VideoTitle));
            RaisePropertyChanged(nameof(Tags));
            RaisePropertyChanged(nameof(ThumbnailUrl));
            RaisePropertyChanged(nameof(VideoLength));
            RaisePropertyChanged(nameof(SubmitDate));
            RaisePropertyChanged(nameof(ViewCount));
            RaisePropertyChanged(nameof(CommentCount));
            RaisePropertyChanged(nameof(MylistCount));
            RaisePropertyChanged(nameof(OwnerName));
            RaisePropertyChanged(nameof(OwnerIconUrl));


            try
            {
                DescriptionHtmlFileUri = await Helpers.HtmlFileHelper.PartHtmlOutputToCompletlyHtml(VideoId, videoDescriptionHtml);
                RaisePropertyChanged(nameof(DescriptionHtmlFileUri));
            }
            catch
            {
                IsLoadFailed.Value = true;
                return;
            }

            try
            {
                if (_VideoInfo != null)
                {
                    SelfZoningInfo = HohoemaApp.UserSettings.NGSettings.IsNgVideo(_VideoInfo);
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
}
