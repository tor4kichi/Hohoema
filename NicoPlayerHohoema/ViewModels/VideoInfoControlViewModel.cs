using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Searches.Video;
using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Models;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using System.Diagnostics;
using Windows.UI.Core;
using NicoPlayerHohoema.Services.Helpers;
using NicoPlayerHohoema.Models.Cache;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Models.LocalMylist;
using NicoPlayerHohoema.Models.Subscription;

namespace NicoPlayerHohoema.ViewModels
{

    public class VideoInfoControlViewModel : HohoemaListingPageItemBase, Interfaces.IVideoContent, Views.Extensions.ListViewBase.IDeferInitialize
    {
        public VideoInfoControlViewModel(
            HohoemaPlaylist hohoemaPlaylist,
            ExternalAccessService externalAccessService,
            PageManager pageManager,
            UserMylistManager userMylistManager,
            LocalMylistManager localMylistManager,
            SubscriptionManager subscriptionManager,
            VideoCacheManager videoCacheManager,
            NicoVideoProvider nicoVideoProvider,
            NGSettings ngSettings,
            Commands.Mylist.CreateMylistCommand createMylistCommand,
            Commands.Mylist.CreateLocalMylistCommand createLocalMylistCommand,
            Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand,
            Commands.AddToHiddenUserCommand addToHiddenUserCommand
            )
        {
            NgSettings = ngSettings;
            CreateMylistCommand = createMylistCommand;
            CreateLocalMylistCommand = createLocalMylistCommand;
            CreateSubscriptionGroupCommand = createSubscriptionGroupCommand;
            AddToHiddenUserCommand = addToHiddenUserCommand;
            HohoemaPlaylist = hohoemaPlaylist;
            ExternalAccessService = externalAccessService;
            PageManager = pageManager;
            UserMylistManager = userMylistManager;
            LocalMylistManager = localMylistManager;
            SubscriptionManager = subscriptionManager;
            VideoCacheManager = videoCacheManager;
            NicoVideoProvider = nicoVideoProvider;

            _CompositeDisposable = new CompositeDisposable();
        }


        public string RawVideoId { get; internal set; }
        public Database.NicoVideo Data { get; internal set; }

        protected CompositeDisposable _CompositeDisposable { get; private set; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public ExternalAccessService ExternalAccessService { get; }
        public PageManager PageManager { get; }
        public UserMylistManager UserMylistManager { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public SubscriptionManager SubscriptionManager { get; }
        public VideoCacheManager VideoCacheManager { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public NGSettings NgSettings { get; }
        public Commands.Mylist.CreateMylistCommand CreateMylistCommand { get; }
        public Commands.Mylist.CreateLocalMylistCommand CreateLocalMylistCommand { get; }
        public Commands.Subscriptions.CreateSubscriptionGroupCommand CreateSubscriptionGroupCommand { get; }
        public Commands.AddToHiddenUserCommand AddToHiddenUserCommand { get; }

        public string Id => RawVideoId;


        public string ProviderId { get; private set; }
        public string ProviderName { get; private set; }
        public UserType ProviderType { get; private set; }

        public VideoStatus VideoStatus { get; private set; }

        public bool IsCacheEnabled { get; private set; }
        public ReactiveProperty<bool> IsCacheRequested { get; } = new ReactiveProperty<bool>(false);

        public ObservableCollection<CachedQualityNicoVideoListItemViewModel> CachedQualityVideos { get; } = new ObservableCollection<CachedQualityNicoVideoListItemViewModel>();

        bool Views.Extensions.ListViewBase.IDeferInitialize.IsInitialized { get; set; }

        async Task Views.Extensions.ListViewBase.IDeferInitialize.DeferInitializeAsync()
        {
            if (Data?.Title != null)
            {
                SetTitle(Data.Title);
            }

            _ = RefrechCacheState();

            await Task.Run(async () =>
            {
                if (IsDisposed)
                {
                    Debug.WriteLine("skip thumbnail loading: " + RawVideoId);
                    return;
                }

                if (NicoVideoProvider != null)
                {
                    Data = await NicoVideoProvider.GetNicoVideoInfo(RawVideoId);
                }

                // オフライン時はローカルDBの情報を利用する
                if (Data == null)
                {
                    Data = Database.NicoVideoDb.Get(RawVideoId);
                }
            });

            if (IsDisposed)
            {
                Debug.WriteLine("skip thumbnail loading: " + RawVideoId);
                return;
            }

            SetupFromThumbnail(Data);
        }

        public async Task RefrechCacheState()
        {
            // キャッシュの状態を更新
            ClearCacheQuality();

            var cacheManager = App.Current.Container.Resolve<VideoCacheManager>();
            var cacheRequests = await cacheManager.GetCacheRequest(RawVideoId);
            IsCacheRequested.Value = cacheRequests.Any();
            if (IsCacheRequested.Value)
            {
                ThemeColor = Windows.UI.Colors.Green;
            }
            else
            {
                if (Database.VideoPlayedHistoryDb.IsVideoPlayed(RawVideoId))
                {
                    // 視聴済み
                    ThemeColor = Windows.UI.Colors.Transparent;
                }
                else
                {
                    // 未視聴
                    ThemeColor = Windows.UI.Colors.Gray;
                }
            }

            foreach (var req in cacheRequests)
            {
                var vm = new CachedQualityNicoVideoListItemViewModel(req, cacheManager);
                CachedQualityVideos.Add(vm);
            }
        }


        bool _isTitleNgCheckProcessed = false;
        bool _isOwnerIdNgCheckProcessed = false;

        public void SetupFromThumbnail(Database.NicoVideo info)
        {
            Debug.WriteLine("thumbnail reflect : " + info.RawVideoId);
            
            Label = info.Title;

            // NG判定
            if (NgSettings != null)
            {
                NGResult ngResult = null;

                // タイトルをチェック
                if (!_isTitleNgCheckProcessed && !string.IsNullOrEmpty(info.Title))
                {
                    ngResult = NgSettings.IsNGVideoTitle(info.Title);
                    _isTitleNgCheckProcessed = true;
                }

                // 投稿者IDをチェック
                if (ngResult == null && 
                    !_isOwnerIdNgCheckProcessed && 
                    !string.IsNullOrEmpty(info.Owner?.OwnerId)
                    )
                {
                    ngResult = NgSettings.IsNgVideoOwnerId(info.Owner.OwnerId);
                    _isOwnerIdNgCheckProcessed = true;
                }

                if (ngResult != null)
                {
                    IsVisible = false;
                    var ngDesc = !string.IsNullOrWhiteSpace(ngResult.NGDescription) ? ngResult.NGDescription : ngResult.Content;
                    InvisibleDescription = $"NG動画";
                }
            }
                        
            SetTitle(info.Title);
            SetThumbnailImage(info.ThumbnailUrl);
            SetSubmitDate(info.PostedAt);
            SetVideoDuration(info.Length);
            if (!info.IsDeleted)
            {
                SetDescription(info.ViewCount, info.CommentCount, info.MylistCount);
            }
            else
            {
                if (info.PrivateReasonType != PrivateReasonType.None)
                {
                    Description = info.PrivateReasonType.ToCulturelizeString();
                }
                else
                {
                    Description = "視聴不可（配信終了など）";
                }
            }

            if (info.Owner != null)
            {
                ProviderId = info.Owner.OwnerId;
                ProviderName = info.Owner.ScreenName;
                ProviderType = info.Owner.UserType;
            }

        }

        internal void SetDescription(int viewcount, int commentCount, int mylistCount)
        {
            Description = $"再生:{viewcount.ToString("N0")} コメ:{commentCount.ToString("N0")} マイ:{mylistCount.ToString("N0")}";
        }

        internal void SetTitle(string title)
        {
            Label = title;
        }
        internal void SetSubmitDate(DateTime submitDate)
        {
            OptionText = submitDate.ToString("yyyy/MM/dd HH:mm");
        }

        internal void SetVideoDuration(TimeSpan duration)
        {
            string timeText;
            if (duration.Hours > 0)
            {
                timeText = duration.ToString(@"hh\:mm\:ss");
            }
            else
            {
                timeText = duration.ToString(@"mm\:ss");
            }
            ImageCaption = timeText;
        }

        internal void SetThumbnailImage(string thumbnailImage)
        {
            if (!string.IsNullOrWhiteSpace(thumbnailImage))
            {
                AddImageUrl(thumbnailImage);
            }
        }

        protected override void OnDispose()
		{
			_CompositeDisposable?.Dispose();

            ClearCacheQuality();

            base.OnDispose();
		}


        private void ClearCacheQuality()
        {
            foreach (var cached in CachedQualityVideos)
            {
                cached.Dispose();
            }

            CachedQualityVideos.Clear();
        }



		protected virtual VideoPlayPayload MakeVideoPlayPayload()
		{
			return new VideoPlayPayload()
			{
				VideoId = RawVideoId,
				Quality = null,
			};
		}



        
        public void SetupDisplay(Mntone.Nico2.Users.Video.VideoData data)
        {
            RawVideoId = data.VideoId;

            SetTitle(data.Title);
            SetThumbnailImage(data.ThumbnailUrl.OriginalString);
            SetSubmitDate(data.SubmitTime);
            SetVideoDuration(data.Length);
        }


        // とりあえずマイリストから取得したデータによる初期化
        public void SetupDisplay(MylistData data)
        {
            RawVideoId = data.WatchId;

            SetTitle(data.Title);
            SetThumbnailImage(data.ThumbnailUrl.OriginalString);
            SetSubmitDate(data.CreateTime);
            SetVideoDuration(data.Length);
            SetDescription((int)data.ViewCount, (int)data.CommentCount, (int)data.MylistCount);
        }


        // 個別マイリストから取得したデータによる初期化
        public void SetupDisplay(VideoInfo data)
        {
            RawVideoId = data.Video.Id;

            SetTitle(data.Video.Title);
            SetThumbnailImage(data.Video.ThumbnailUrl.OriginalString);
            SetSubmitDate(data.Video.UploadTime);
            SetVideoDuration(data.Video.Length);
            SetDescription((int)data.Video.ViewCount, (int)data.Thread.GetCommentCount(), (int)data.Video.MylistCount);
        }

    }


    public class CachedQualityNicoVideoListItemViewModel : BindableBase, IDisposable
    {
        public NicoVideoQuality Quality { get; private set; }

        public IReadOnlyReactiveProperty<bool> IsCached { get; private set; }
        public IReadOnlyReactiveProperty<NicoVideoCacheState> CacheState { get; private set; }
        public IReadOnlyReactiveProperty<bool> IsCacheDownloading { get; }
        public IReactiveProperty<float> ProgressPercent { get; private set; }

        NicoVideoCacheRequest _Request;

        private CompositeDisposable _CompositeDisposable = new CompositeDisposable();


        public CachedQualityNicoVideoListItemViewModel(NicoVideoCacheRequest req, VideoCacheManager cacheManager)
        {
            _Request = req;
            Quality = _Request.Quality;

            var firstCacheState = _Request.ToCacheState();

            if (firstCacheState != NicoVideoCacheState.Cached)
            {
                CacheState = Observable.FromEventPattern<VideoCacheStateChangedEventArgs>(
                    (x) => cacheManager.VideoCacheStateChanged += x,
                    (x) => cacheManager.VideoCacheStateChanged -= x
                    )
                    .Where(x => x.EventArgs.Request.RawVideoId == _Request.RawVideoId && x.EventArgs.Request.Quality == _Request.Quality)
                    .Select(x => x.EventArgs.CacheState)
                    .ObserveOnUIDispatcher()
                    .ToReadOnlyReactiveProperty(firstCacheState)
                    .AddTo(_CompositeDisposable);

                CacheState.Subscribe(x =>
                {
                    if (x == NicoVideoCacheState.Downloading)
                    {
                        var _cacheManager = App.Current.Container.Resolve<VideoCacheManager>();

                        float firstProgressParcent = 0.0f;
                        if (_Request is NicoVideoCacheProgress)
                        {
                            var prog = (_Request as NicoVideoCacheProgress).DownloadOperation.Progress;
                            if (prog.TotalBytesToReceive > 0)
                            {
                                firstProgressParcent = (float)Math.Round((prog.BytesReceived / (float)prog.TotalBytesToReceive) * 100, 1);
                            }
                        }
                        ProgressPercent = Observable.FromEventPattern<NicoVideoCacheProgress>(
                            (handler) => _cacheManager.DownloadProgress += handler,
                            (handler) => _cacheManager.DownloadProgress -= handler
                            )
                            .ObserveOnUIDispatcher()
                            .Where(y => y.EventArgs.RawVideoId == _Request.RawVideoId && y.EventArgs.Quality == _Request.Quality)
                            .Select(y =>
                            {
                                var prog = y.EventArgs.DownloadOperation.Progress;
                                if (prog.TotalBytesToReceive > 0)
                                {
                                    return (float)Math.Round((prog.BytesReceived / (float)prog.TotalBytesToReceive) * 100, 1);
                                }
                                else
                                {
                                    return 0.0f;
                                }
                            })
                            .ToReactiveProperty(firstProgressParcent);
                        RaisePropertyChanged(nameof(ProgressPercent));
                    }
                    else
                    {
                        ProgressPercent?.Dispose();
                        ProgressPercent = null;
                        RaisePropertyChanged(nameof(ProgressPercent));
                    }
                })
                .AddTo(_CompositeDisposable);

                IsCacheDownloading = CacheState.Select(x => x == NicoVideoCacheState.Downloading)
                    .ToReadOnlyReactiveProperty()
                    .AddTo(_CompositeDisposable);
            }
            else
            {
                CacheState = new ReactiveProperty<NicoVideoCacheState>(NicoVideoCacheState.Cached);
                IsCacheDownloading = new ReactiveProperty<bool>(false);
                ProgressPercent = new ReactiveProperty<float>(0.0f);
            }

            IsCached = CacheState.Select(x => x == NicoVideoCacheState.Cached)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(_CompositeDisposable);
        }

        public void Dispose()
        {
            _CompositeDisposable?.Dispose();
            _CompositeDisposable = null;

            ProgressPercent?.Dispose();
        }
    }





    [Flags]
    public enum VideoStatus
    {
        Watched = 0x0001,
        Filtered = 0x1000,
    }
}
