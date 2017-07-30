using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using Prism.Commands;
using NicoPlayerHohoema.Util;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Practices.Unity;
using Prism.Windows.Navigation;
using System.Threading;
using System.Diagnostics;
using Mntone.Nico2;
using Mntone.Nico2.Embed.Ichiba;

namespace NicoPlayerHohoema.ViewModels
{
    public class VideoInfomationPageViewModel : HohoemaViewModelBase
    {
        public Uri DescriptionHtmlFileUri { get; private set; }

        public NicoVideo Video { get; private set; }

        public string VideoTitle { get; private set; }

        public string ThumbnailUrl { get; private set; }

        public IList<TagViewModel> Tags { get; private set; }

        public string OwnerName { get; private set; }
        public string OwnerIconUrl { get; private set; }

        public TimeSpan VideoLength { get; private set; }
        
        public DateTime SubmitDate { get; private set; }

        public uint ViewCount { get; private set; }
        public uint CommentCount { get; private set; }
        public uint MylistCount { get; private set; }

        public Uri VideoPageUri { get; private set; }

        public ReactiveProperty<bool> NowLoading { get; private set; }
        public ReactiveProperty<bool> IsLoadFailed { get; private set; }



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
                        PageManager.OpenPage(HohoemaPageType.Settings, HohoemaSettingsKind.Filtering.ToString());
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
                        PageManager.OpenPage(HohoemaPageType.UserInfo, Video.OwnerId.ToString());
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
                        PageManager.OpenPage(HohoemaPageType.UserVideo, Video.OwnerId.ToString());
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
                        HohoemaApp.Playlist.PlayVideo(Video.RawVideoId, Video.Title);
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
                        Video.RequestCache();
                    }
                    ));
            }
        }



        public ReactiveProperty<bool> IsStillLoggedInTwitter { get; private set; }

        private DelegateCommand _ShereWithTwitterCommand;
        public DelegateCommand ShereWithTwitterCommand
        {
            get
            {
                return _ShereWithTwitterCommand
                    ?? (_ShereWithTwitterCommand = new DelegateCommand(async () =>
                    {
                        if (!TwitterHelper.IsLoggedIn)
                        {

                            if (!await TwitterHelper.LoginOrRefreshToken())
                            {
                                return;
                            }
                        }

                        IsStillLoggedInTwitter.Value = !TwitterHelper.IsLoggedIn;

                        var textInputDialogService = App.Current.Container.Resolve<Views.Service.TextInputDialogService>();

                        if (TwitterHelper.IsLoggedIn)
                        {
                            var text = $"{Video.Title} http://nico.ms/{Video.VideoId} #{Video.VideoId}";
                            var twitterLoginUserName = TwitterHelper.TwitterUser.ScreenName;
                            var customText = await textInputDialogService.GetTextAsync($"{twitterLoginUserName} としてTwitterへ投稿", "", text);

                            if (customText != null)
                            {
                                var result = await TwitterHelper.SubmitTweet(customText);

                                if (!result)
                                {
                                    var toastService = App.Current.Container.Resolve<Views.Service.ToastNotificationService>();
                                    toastService.ShowText("ツイートに失敗しました", "もう一度お試しください");
                                }
                            }
                        }
                    }
                    ));
            }
        }

        private DelegateCommand _ShareWithClipboardCommand;
        public DelegateCommand ShareWithClipboardCommand
        {
            get
            {
                return _ShareWithClipboardCommand
                    ?? (_ShareWithClipboardCommand = new DelegateCommand(() =>
                    {
                        var videoUrl = $"http://nico.ms/{Video.VideoId}";
                        var text = $"{Video.Title} {videoUrl} #{Video.VideoId}";
                        var datapackage = new DataPackage();
                        datapackage.SetText(text);
                        datapackage.SetWebLink(new Uri(videoUrl));

                        Clipboard.SetContent(datapackage);
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
                        var mylistResistrationDialogService = App.Current.Container.Resolve<Views.Service.MylistRegistrationDialogService>();

                        var groupAndComment = await mylistResistrationDialogService.ShowDialog(1);
                        if (groupAndComment != null)
                        {
                            await groupAndComment.Item1.Registration(Video.RawVideoId, groupAndComment.Item2);
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

                        playlist.AddVideo(Video.RawVideoId, Video.Title);
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
            : base(hohoemaApp, pageManager, canActivateBackgroundUpdate:true)
        {
            ChangeRequireServiceLevel(HohoemaAppServiceLevel.OnlineWithoutLoggedIn);

            IsStillLoggedInTwitter = new ReactiveProperty<bool>(Util.TwitterHelper.IsLoggedIn);
            NowLoading = new ReactiveProperty<bool>(false);
            IsLoadFailed = new ReactiveProperty<bool>(false);

        }


        protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (e.Parameter is string)
            {
                var videoId = e.Parameter as string;
                Video = await HohoemaApp.MediaManager.GetNicoVideoAsync(videoId);
            }

            if (Video == null)
            {
                IsLoadFailed.Value = true;
                throw new Exception();
            }

            VideoPageUri = new Uri("http://nicovideo.jp/watch/" + Video.RawVideoId);
            OnPropertyChanged(nameof(VideoPageUri));


            await Update();

            try
            {
                var ichiba = await HohoemaApp.NiconicoContext.Embed.GetIchiba(Video.RawVideoId);
                IchibaItems = ichiba.GetMainIchibaItems();
                if (IchibaItems.Count > 0)
                {
                    OnPropertyChanged(nameof(IchibaItems));
                }
            }
            catch
            {
                Debug.WriteLine(Video.RawVideoId + " の市場情報の取得に失敗");
            }
        }


        private async Task Update()
        {
            if (Video == null)
            {
                return;
            }

            NowLoading.Value = true;
            IsLoadFailed.Value = false;

            try
            {
                try
                {
                    if (Video.ContentType == Mntone.Nico2.Videos.Thumbnail.MovieType.Flv)
                    {
                        await HohoemaApp.NiconicoContext.Video.GetWatchApiAsync(Video.RawVideoId, false);
                    }
                    else
                    {
                        await Video.SetupWatchPageVisit(NicoVideoQuality.Low);

                    }


                    VideoTitle = Video.Title;
                    Tags = Video.Tags.Select(x => new TagViewModel(x.Value, PageManager))
                        .ToList();
                    ThumbnailUrl = Video.ThumbnailUrl;
                    VideoLength = Video.VideoLength;
                    SubmitDate = Video.PostedAt;
                    ViewCount = Video.ViewCount;
                    CommentCount = Video.CommentCount;
                    MylistCount = Video.MylistCount;
                }
                catch
                {
                    IsLoadFailed.Value = true;
                    return;
                }



                OnPropertyChanged(nameof(VideoTitle));
                OnPropertyChanged(nameof(Tags));
                OnPropertyChanged(nameof(ThumbnailUrl));
                OnPropertyChanged(nameof(VideoLength));
                OnPropertyChanged(nameof(SubmitDate));
                OnPropertyChanged(nameof(ViewCount));
                OnPropertyChanged(nameof(CommentCount));
                OnPropertyChanged(nameof(MylistCount));


                try
                {
                    DescriptionHtmlFileUri = await Util.HtmlFileHelper.PartHtmlOutputToCompletlyHtml(Video.RawVideoId, Video.DescriptionWithHtml);
                    OnPropertyChanged(nameof(DescriptionHtmlFileUri));
                }
                catch
                {
                    IsLoadFailed.Value = true;
                    return;
                }

                try
                {
                    OwnerName = Video.OwnerName;
                    OwnerIconUrl = Video.OwnerIconUrl;

                    OnPropertyChanged(nameof(OwnerName));
                    OnPropertyChanged(nameof(OwnerIconUrl));
                }
                catch
                {
                    IsLoadFailed.Value = true;
                    return;
                }

                try
                {
                    Playlists = HohoemaApp.Playlist.Playlists.ToList();
                }
                catch
                {
                    IsLoadFailed.Value = true;
                    return;
                }


                SelfZoningInfo = Video.CheckUserNGVideo();
                IsSelfZoningContent = SelfZoningInfo != null;

                OnPropertyChanged(nameof(SelfZoningInfo));
                OnPropertyChanged(nameof(IsSelfZoningContent));
            }
            finally
            {
                NowLoading.Value = false;
            }
        }


        
    }
}
