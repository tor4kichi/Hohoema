using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Mylist.MylistGroup;
using Mntone.Nico2.Searches.Video;
using Mntone.Nico2.Videos.Ranking;
using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Db;
using NicoPlayerHohoema.Helpers;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Practices.Unity;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Popups;
using NicoPlayerHohoema.Views.Service;
using System.Diagnostics;

namespace NicoPlayerHohoema.ViewModels
{
    
	public class VideoInfoControlViewModel : HohoemaListingPageItemBase, Interfaces.IVideoContent
    {
        public string Id => RawVideoId;


        public string RawVideoId { get; private set; }
        public string OwnerUserId { get; private set; }
        public string OwnerUserName { get; private set; }

        public IPlayableList Playlist => PlaylistItem?.Owner;

        public VideoStatus VideoStatus { get; private set; }

        public bool IsCacheEnabled { get; private set; }
        public ReactiveProperty<bool> IsCacheRequested { get; } = new ReactiveProperty<bool>(false);

        public ObservableCollection<CachedQualityNicoVideoListItemViewModel> CachedQualityVideos { get; } = new ObservableCollection<CachedQualityNicoVideoListItemViewModel>();


        public PlaylistItem PlaylistItem { get; }


        protected CompositeDisposable _CompositeDisposable { get; private set; }

        static Helpers.AsyncLock _DefferedUpdateLock = new Helpers.AsyncLock();

        bool _IsNGEnabled = false;

        public VideoInfoControlViewModel(string videoId, bool isNgEnabled = true, PlaylistItem playlistItem = null)
        {
            RawVideoId = videoId;
            PlaylistItem = playlistItem;
            _CompositeDisposable = new CompositeDisposable();

            _IsNGEnabled = isNgEnabled;

            OnDeferredUpdate().ConfigureAwait(false);
        }

        public VideoInfoControlViewModel(Database.NicoVideo nicoVideo, bool isNgEnabled = true, PlaylistItem playlistItem = null)
        {
            RawVideoId = nicoVideo.RawVideoId;
            PlaylistItem = playlistItem;
            _CompositeDisposable = new CompositeDisposable();

            _IsNGEnabled = isNgEnabled;

            SetupFromThumbnail(nicoVideo);

            OnDeferredUpdate().ConfigureAwait(false);
        }

        protected override async Task OnDeferredUpdate()
        {
            var contentProvider = App.Current.Container.Resolve<NiconicoContentProvider>();
            var info = await contentProvider.GetNicoVideoInfo(RawVideoId);

            // Note: 動画リストの一覧表示が終わってからサムネイル情報読み込みが掛かるようにする
            using (var releaser = await _DefferedUpdateLock.LockAsync())
            {
                await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    SetupFromThumbnail(info);

                    await RefrechCacheState();
                });
            }
        }

        protected override void OnCancelDeferrdUpdate()
        {
            // TODO: キャンセルの実装
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
                if (VideoPlayHistoryDb.Get(RawVideoId).PlayCount > 0)
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
            if (_IsNGEnabled)
            {
                var hohoemaApp = App.Current.Container.Resolve<HohoemaApp>();

                NGResult ngResult = null;

                // タイトルをチェック
                if (!_isTitleNgCheckProcessed && !string.IsNullOrEmpty(info.Title))
                {
                    ngResult = hohoemaApp.UserSettings.NGSettings.IsNGVideoTitle(info.Title);
                    _isTitleNgCheckProcessed = true;
                }

                // 投稿者IDをチェック
                if (ngResult == null && 
                    !_isOwnerIdNgCheckProcessed && 
                    !string.IsNullOrEmpty(info.Owner?.OwnerId)
                    )
                {
                    ngResult = hohoemaApp.UserSettings.NGSettings.IsNgVideoOwnerId(info.Owner.OwnerId);
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
                OwnerUserId = info.Owner.OwnerId;
                OwnerUserName = info.Owner.ScreenName;
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
