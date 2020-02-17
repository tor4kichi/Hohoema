using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Searches.Video;
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
using Unity;
using System.Diagnostics;
using Windows.UI.Core;
using NicoPlayerHohoema.Services.Helpers;
using NicoPlayerHohoema.Models.Cache;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Models.LocalMylist;
using NicoPlayerHohoema.Models.Subscription;
using NicoPlayerHohoema.Services.Page;
using System.Reactive.Concurrency;
using NicoPlayerHohoema.Commands.Mylist;
using NicoPlayerHohoema.Commands.Subscriptions;
using NicoPlayerHohoema.Commands;
using NicoPlayerHohoema.Commands.Cache;
using Prism.Commands;
using Prism.Unity;
using NicoPlayerHohoema.Interfaces;
using I18NPortable;

namespace NicoPlayerHohoema.ViewModels
{

    public class VideoInfoControlViewModel : HohoemaListingPageItemBase, Interfaces.IVideoContentWritable, Views.Extensions.ListViewBase.IDeferInitialize
    {
        public VideoInfoControlViewModel(Database.NicoVideo data)            
        {
            VideoCacheManager = App.Current.Container.Resolve<VideoCacheManager>();
            NicoVideoProvider = App.Current.Container.Resolve<NicoVideoProvider>();
            NgSettings = App.Current.Container.Resolve<NGSettings>();

            RawVideoId = data?.RawVideoId ?? RawVideoId;
            Data = data;

            if (Data != null)
            {
                SetupFromThumbnail(Data);
            }
        }


        public VideoInfoControlViewModel(
            string rawVideoId
            )
            : this(data: null)
        {
            RawVideoId = rawVideoId;
        }

        public bool Equals(IVideoContent other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }


        public NicoVideoProvider NicoVideoProvider { get; }
        public VideoCacheManager VideoCacheManager { get; }
        public NGSettings NgSettings { get; }

        public string RawVideoId { get; }
        public Database.NicoVideo Data { get; private set; }

        public string Id => RawVideoId;


        public string ProviderId { get; set; }
        public string ProviderName { get; set; }
        public Database.NicoVideoUserType ProviderType { get; set; }

        public Interfaces.IMylist OnwerPlaylist { get; }

        public VideoStatus VideoStatus { get; private set; }

        bool Views.Extensions.ListViewBase.IDeferInitialize.IsInitialized { get; set; }

        public TimeSpan Length { get; set; }

        public DateTime PostedAt { get; set; }

        public int ViewCount { get; set; }

        public int MylistCount { get; set; }

        public int CommentCount { get; set; }

        public string ThumbnailUrl { get; set; }
        public bool IsDeleted { get; set; }

        bool IVideoContent.IsDeleted => IsDeleted;

        async Task Views.Extensions.ListViewBase.IDeferInitialize.DeferInitializeAsync()
        {
            if (Data?.Title != null)
            {
                SetTitle(Data.Title);
            }

            _ = RefrechCacheState();

            var data = await Task.Run(async () =>
            {
                if (IsDisposed)
                {
                    Debug.WriteLine("skip thumbnail loading: " + RawVideoId);
                    return null;
                }

                if (NicoVideoProvider != null)
                {
                    return await NicoVideoProvider.GetNicoVideoInfo(RawVideoId);
                }

                // オフライン時はローカルDBの情報を利用する
                if (Data == null)
                {
                    return Database.NicoVideoDb.Get(RawVideoId);
                }

                return null;
            });

            if (data == null) { return; }

            Data = data;

            if (IsDisposed)
            {
                Debug.WriteLine("skip thumbnail loading: " + RawVideoId);
                return;
            }

            SetupFromThumbnail(Data);
        }

        public async Task RefrechCacheState()
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


        bool _isTitleNgCheckProcessed = false;
        bool _isOwnerIdNgCheckProcessed = false;

        public void SetupFromThumbnail(Database.NicoVideo info)
        {
            Debug.WriteLine("thumbnail reflect : " + info.RawVideoId);
            
            Label = info.Title;
            PostedAt = info.PostedAt;
            Length = info.Length;
            ViewCount = info.ViewCount;
            MylistCount = info.MylistCount;
            CommentCount = info.CommentCount;
            ThumbnailUrl = info.ThumbnailUrl;

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
                    Description = info.PrivateReasonType.Translate();
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
            PostedAt = submitDate;
        }

        internal void SetVideoDuration(TimeSpan duration)
        {
            Length = duration;
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
            if (data.VideoId != RawVideoId) { throw new Exception(); }

            SetTitle(data.Title);
            SetThumbnailImage(data.ThumbnailUrl.OriginalString);
            SetSubmitDate(data.SubmitTime);
            SetVideoDuration(data.Length);
        }


        // とりあえずマイリストから取得したデータによる初期化
        public void SetupDisplay(MylistData data)
        {
            if (data.WatchId != RawVideoId) { throw new Exception(); }

            SetTitle(data.Title);
            SetThumbnailImage(data.ThumbnailUrl.OriginalString);
            SetSubmitDate(data.CreateTime);
            SetVideoDuration(data.Length);
            SetDescription((int)data.ViewCount, (int)data.CommentCount, (int)data.MylistCount);
        }


        // 個別マイリストから取得したデータによる初期化
        public void SetupDisplay(VideoInfo data)
        {
            if (data.Video.Id != RawVideoId) { throw new Exception(); }


            SetTitle(data.Video.Title);
            SetThumbnailImage(data.Video.ThumbnailUrl.OriginalString);
            SetSubmitDate(data.Video.UploadTime);
            SetVideoDuration(data.Video.Length);
            SetDescription((int)data.Video.ViewCount, (int)data.Thread.GetCommentCount(), (int)data.Video.MylistCount);
        }

    }






    [Flags]
    public enum VideoStatus
    {
        Watched = 0x0001,
        Filtered = 0x1000,
    }
}
