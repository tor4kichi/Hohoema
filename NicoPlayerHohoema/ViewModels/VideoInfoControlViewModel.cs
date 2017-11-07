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
        //	private IScheduler scheduler;



        public string Id => RawVideoId;

        public string RawVideoId { get; private set; }

        
        public string OwnerUserId { get; private set; }
            

        public string OwnerUserName { get; private set; }

        public IPlayableList Playlist => PlaylistItem?.Owner;

        public VideoStatus VideoStatus { get; private set; }


        public bool IsXbox => Helpers.DeviceTypeHelper.IsXbox;

        public bool IsCacheEnabled { get; private set; }
        public ReadOnlyReactiveProperty<bool> IsCacheRequested { get; private set; }

        public ReactiveProperty<bool> IsRequireConfirmDelete { get; private set; }
        public string PrivateReasonText { get; private set; }


        private static Helpers.AsyncLock _QualityDividedVideosLock = new Helpers.AsyncLock();
        public ObservableCollection<CachedQualityNicoVideoListItemViewModel> CachedQualityVideos { get; private set; }

        protected CompositeDisposable _CompositeDisposable { get; private set; }

        public PlaylistItem PlaylistItem { get; }


        static Helpers.AsyncLock ThumbnailUpdateLock = new Helpers.AsyncLock();


        bool _IsNGEnabled = false;

        public VideoInfoControlViewModel(string videoId, bool isNgEnabled = true, PlaylistItem playlistItem = null)
        {
            RawVideoId = videoId;
            PlaylistItem = playlistItem;
            _CompositeDisposable = new CompositeDisposable();

            _IsNGEnabled = isNgEnabled;

            var info = Database.NicoVideoDb.Get(videoId);
            if (info != null)
            {
                SetupFromThumbnail(info);
            }

            /*
            Observable.CombineLatest(
                IsCacheRequested,
                NicoVideo.ObserveProperty(x => x.IsPlayed)
                )
                .Select(x =>
                {
                    if (x[0]) { return Windows.UI.Colors.Green; }
                    else if (x[1]) { return Windows.UI.Colors.Transparent; }
                    else { return Windows.UI.Colors.Gray; }
                })
                .ObserveOnUIDispatcher()
                .Subscribe(color => ThemeColor = color)
                .AddTo(_CompositeDisposable);
            */
        }

        protected override async Task OnDeferredUpdate()
        {
            // Note: 動画リストの一覧表示が終わってからサムネイル情報読み込みが掛かるようにする
            var contentProvider = App.Current.Container.Resolve<NiconicoContentProvider>();
            var info = await contentProvider.GetNicoVideoInfo(RawVideoId);
            SetupFromThumbnail(info);            
        }

        protected override void OnCancelDeferrdUpdate()
        {
            // TODO: キャンセルの実装
        }


        private async void ResetQualityDivideVideosVM()
        {
            /*
            using (var releaser = await _QualityDividedVideosLock.LockAsync())
            {
                await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
                {
                    
                    CachedQualityVideos.Clear();

                    foreach (var div in NicoVideo.QualityDividedVideos.ToArray())
                    {
                        var vm = new QualityDividedNicoVideoListItemViewModel(div)
                            .AddTo(_CompositeDisposable);
                        CachedQualityVideos.Add(vm);
                    }
                });
            }
            */
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
                if (!_isOwnerIdNgCheckProcessed && !string.IsNullOrEmpty(info.Owner?.OwnerId))
                {
                    ngResult = hohoemaApp.UserSettings.NGSettings.IsNgVideoOwnerId(info.Owner.OwnerId);
                    _isOwnerIdNgCheckProcessed = true;
                }

                if (ngResult != null)
                {
                    IsVisible = true;
                    var ngDesc = !string.IsNullOrWhiteSpace(ngResult.NGDescription) ? ngResult.NGDescription : ngResult.Content;
                    InvisibleDescription = $"NG動画";
                }
            }
            
            PrivateReasonText = info.PrivateReasonType.ToString() ?? "";            
            
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
                Description = "Deleted";
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
                ImageUrlsSource.Add(thumbnailImage);
            }
        }

        protected override void OnDispose()
		{
			_CompositeDisposable?.Dispose();

            base.OnDispose();
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


    public class CachedQualityNicoVideoListItemViewModel : IDisposable
    {
        public NicoVideoQuality Quality { get; private set; }

        public ReadOnlyReactiveProperty<NicoVideoCacheState> CacheState { get; private set; }

        public ReactiveProperty<float> ProgressPercent { get; private set; }

        IDisposable _ProgressParcentageMoniterDisposer;


        private CompositeDisposable _CompositeDisposable = new CompositeDisposable();

        public CachedQualityNicoVideoListItemViewModel(NicoVideoCacheRequest req, VideoCacheManager cacheManager)
        {
            var firstCacheState = req.ToCacheState();

            CacheState = Observable.FromEventPattern<VideoCacheStateChangedEventArgs>(
                (x) => cacheManager.VideoCacheStateChanged += x,
                (x) => cacheManager.VideoCacheStateChanged -= x
                )
                .Select(x => x.EventArgs.CacheState)
                .ToReadOnlyReactiveProperty(firstCacheState)
                .AddTo(_CompositeDisposable);

            CacheState.Subscribe(x =>
            {
                if (x == NicoVideoCacheState.Downloading)
                {
                    // TODO: 
                }
                else
                {
                    _ProgressParcentageMoniterDisposer?.Dispose();
                    _ProgressParcentageMoniterDisposer = null;
                }
            })
            .AddTo(_CompositeDisposable);
        }

        public void Dispose()
        {
            _CompositeDisposable?.Dispose();
            _CompositeDisposable = null;

            _ProgressParcentageMoniterDisposer?.Dispose();
        }
    }





    [Flags]
    public enum VideoStatus
    {
        Watched = 0x0001,
        Filtered = 0x1000,
    }
}
