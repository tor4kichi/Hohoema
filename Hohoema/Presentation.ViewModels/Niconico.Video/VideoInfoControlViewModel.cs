﻿using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser;
using Hohoema.Models.Domain.Player.Video.Cache;
using Hohoema.Models.Domain.VideoCache;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Models.UseCase.NicoVideos.Events;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Models.UseCase.VideoCache.Events;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.Pages.VideoListPage.Commands;
using Microsoft.Toolkit.Mvvm.Messaging;
using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Searches.Video;
using NiconicoLiveToolkit.Video;
using Prism.Commands;
using Prism.Unity;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity;

namespace Hohoema.Presentation.ViewModels.VideoListPage
{
    public class VideoInfoControlViewModel : FixPrism.BindableBase, IVideoContent, IDisposable, 
        IRecipient<VideoPlayedMessage>,
        IRecipient<QueueItemAddedMessage>,
        IRecipient<QueueItemRemovedMessage>,
        IRecipient<QueueItemIndexUpdateMessage>,
        IRecipient<VideoCacheStatusChangedMessage>,
        IRecipient<VideoCacheProgressChangedMessage>
    {
        static VideoInfoControlViewModel()
        {
            _nicoVideoProvider = App.Current.Container.Resolve<NicoVideoProvider>();
            _hohoemaPlaylist = App.Current.Container.Resolve<HohoemaPlaylist>();
            _ngSettings = App.Current.Container.Resolve<VideoFilteringSettings>();
            _cacheManager = App.Current.Container.Resolve<VideoCacheManager>();
            _scheduler = App.Current.Container.Resolve<IScheduler>();
            _nicoVideoRepository = App.Current.Container.Resolve<NicoVideoCacheRepository>();
            _videoPlayedHistoryRepository = App.Current.Container.Resolve<VideoPlayedHistoryRepository>();
            _userNameProvider = App.Current.Container.Resolve<UserNameProvider>();
            _channelProvider =  App.Current.Container.Resolve<ChannelProvider>();
            _addWatchAfterCommand = App.Current.Container.Resolve<QueueAddItemCommand>();
            _openVideoOwnerPageCommand = App.Current.Container.Resolve<OpenVideoOwnerPageCommand>();

            _removeWatchAfterCommand = App.Current.Container.Resolve<QueueRemoveItemCommand>();
        }


        public VideoInfoControlViewModel(
            string rawVideoId
            )
        {
            RawVideoId = rawVideoId;

            _ngSettings.VideoOwnerFilterAdded += _ngSettings_VideoOwnerFilterAdded;
            _ngSettings.VideoOwnerFilterRemoved += _ngSettings_VideoOwnerFilterRemoved;

            WeakReferenceMessenger.Default.Register<VideoPlayedMessage, string>(this, RawVideoId);
            WeakReferenceMessenger.Default.Register<QueueItemAddedMessage, string>(this, RawVideoId);
            WeakReferenceMessenger.Default.Register<QueueItemRemovedMessage, string>(this, RawVideoId);
            WeakReferenceMessenger.Default.Register<QueueItemIndexUpdateMessage, string>(this, RawVideoId);

            (IsQueueItem, QueueItemIndex) = _hohoemaPlaylist.IsQueuePlaylistItem(RawVideoId);
        }

        public VideoInfoControlViewModel(NicoVideo data)            
            : this(data.RawVideoId)
        {
            Data = data;
            
            _Label = data.Title;
            _PostedAt = data.PostedAt;
            _Length = data.Length;
            _ViewCount = data.ViewCount;
            _MylistCount = data.MylistCount;
            _CommentCount = data.CommentCount;
            _ThumbnailUrl ??= data.ThumbnailUrl;
            _IsDeleted = data.IsDeleted;
            _PrivateReason = Data.PrivateReasonType;
            _Description = Data.Description;
            Permission = Data.Permission;

            if (data.Owner != null)
            {
                ProviderId = data.Owner.OwnerId;
                _ProviderName = data.Owner.ScreenName;
                ProviderType = data.Owner.UserType;
            }

            SubscriptionWatchedIfNotWatch(data);
            UpdateIsHidenVideoOwner(data);
            SubscribeCacheState(data);
        }


        void IRecipient<QueueItemAddedMessage>.Receive(QueueItemAddedMessage message)
        {
            IsQueueItem = true;
        }

        void IRecipient<QueueItemRemovedMessage>.Receive(QueueItemRemovedMessage message)
        {
            IsQueueItem = false;
            QueueItemIndex = -1;
        }


        void IRecipient<QueueItemIndexUpdateMessage>.Receive(QueueItemIndexUpdateMessage message)
        {
            QueueItemIndex = message.Value.Index;
        }


        private void _ngSettings_VideoOwnerFilterRemoved(object sender, VideoOwnerFilteringRemovedEventArgs e)
        {
            if (e.OwnerId == this.ProviderId)
            {
                UpdateIsHidenVideoOwner(Data);
            }
        }

        private void _ngSettings_VideoOwnerFilterAdded(object sender, VideoOwnerFilteringAddedEventArgs e)
        {
            if (e.OwnerId == this.ProviderId)
            {
                UpdateIsHidenVideoOwner(Data);
            }
        }

        public void Dispose()
        {
            _ngSettings.VideoOwnerFilterAdded -= _ngSettings_VideoOwnerFilterAdded;
            _ngSettings.VideoOwnerFilterRemoved -= _ngSettings_VideoOwnerFilterRemoved;

            WeakReferenceMessenger.Default.UnregisterAll(this, RawVideoId);

            UnsubscriptionWatched();
        }



        private void NGVideoOwnerUserIds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateIsHidenVideoOwner(Data);
        }


        void UpdateIsHidenVideoOwner(IVideoContent video)
        {
            if (video != null)
            {
                _ngSettings.TryGetHiddenReason(video, out var result);
                VideoHiddenInfo = result;
            }
            else
            {
                VideoHiddenInfo = null;
            }
        }

        private DelegateCommand _UnregistrationHiddenVideoOwnerCommand;
        public DelegateCommand UnregistrationHiddenVideoOwnerCommand =>
            _UnregistrationHiddenVideoOwnerCommand ?? (_UnregistrationHiddenVideoOwnerCommand = new DelegateCommand(ExecuteUnregistrationHiddenVideoOwnerCommand));

        void ExecuteUnregistrationHiddenVideoOwnerCommand()
        {
            if (Data != null)
            {
                _ngSettings.RemoveHiddenVideoOwnerId(Data.ProviderId);
            }

        }


        public bool Equals(IVideoContent other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }


        private static readonly NicoVideoProvider _nicoVideoProvider;
        private static readonly HohoemaPlaylist _hohoemaPlaylist;
        private static readonly NicoVideoCacheRepository _nicoVideoRepository;
        private static readonly VideoPlayedHistoryRepository _videoPlayedHistoryRepository;
        private static readonly UserNameProvider _userNameProvider;
        private static readonly ChannelProvider _channelProvider;
        private static readonly VideoFilteringSettings _ngSettings;
        private static readonly VideoCacheManager _cacheManager;
        private static readonly IScheduler _scheduler;

        private static readonly QueueAddItemCommand _addWatchAfterCommand;
        public QueueAddItemCommand AddWatchAfterCommand => _addWatchAfterCommand;

        private static readonly QueueRemoveItemCommand _removeWatchAfterCommand;
        public QueueRemoveItemCommand RemoveWatchAfterCommand => _removeWatchAfterCommand;

        private static readonly OpenVideoOwnerPageCommand _openVideoOwnerPageCommand;

        public OpenVideoOwnerPageCommand OpenVideoOwnerPageCommand => _openVideoOwnerPageCommand;


        private bool _IsInitialized;
        public bool IsInitialized
        {
            get { return _IsInitialized; }
            set { SetProperty(ref _IsInitialized, value); }
        }


        public string RawVideoId { get; }
        public NicoVideo Data { get; private set; }

        public string Id => RawVideoId;


        public string ProviderId { get; set; }
        
        private string _ProviderName;
        public string ProviderName
        {
            get { return _ProviderName; }
            set { SetProperty(ref _ProviderName, value); }
        }

        private bool _IsQueueItem;
        public bool IsQueueItem
        {
            get { return _IsQueueItem; }
            set { SetProperty(ref _IsQueueItem, value); }
        }

        private int _QueueItemIndex;
        public int QueueItemIndex
        {
            get { return _QueueItemIndex; }
            set { SetProperty(ref _QueueItemIndex, value + 1); }
        }

        public NicoVideoUserType ProviderType { get; set; }

        public IMylist OnwerPlaylist { get; }

        public VideoStatus VideoStatus { get; private set; }


        private string _Label;
        public string Label
        {
            get { return _Label; }
            set { SetProperty(ref _Label, value); }
        }

        private TimeSpan _Length;
        public TimeSpan Length
        {
            get { return _Length; }
            set { SetProperty(ref _Length, value); }
        }


        private string _Description;
        public string Description
        {
            get { return _Description; }
            set { SetProperty(ref _Description, value); }
        }

        private DateTime _PostedAt;
        public DateTime PostedAt
        {
            get { return _PostedAt; }
            set { SetProperty(ref _PostedAt, value); }
        }


        private int _ViewCount;
        public int ViewCount
        {
            get { return _ViewCount; }
            set { SetProperty(ref _ViewCount, value); }
        }


        private int _MylistCount;
        public int MylistCount
        {
            get { return _MylistCount; }
            set { SetProperty(ref _MylistCount, value); }
        }

        private int _CommentCount;
        public int CommentCount
        {
            get { return _CommentCount; }
            set { SetProperty(ref _CommentCount, value); }
        }

        private string _ThumbnailUrl;
        public string ThumbnailUrl
        {
            get { return _ThumbnailUrl; }
            set { SetProperty(ref _ThumbnailUrl, value); }
        }


        private bool _IsDeleted;
        public bool IsDeleted
        {
            get { return _IsDeleted; }
            set { SetProperty(ref _IsDeleted, value); }
        }


        private FilteredResult _VideoHiddenInfo;
        public FilteredResult VideoHiddenInfo
        {
            get { return _VideoHiddenInfo; }
            set { SetProperty(ref _VideoHiddenInfo, value); }
        }

        private bool _IsWatched;
        public bool IsWatched
        {
            get { return _IsWatched; }
            set { SetProperty(ref _IsWatched, value); }
        }

       

        private VideoPermission _permission;
        public VideoPermission Permission
        {
            get { return _permission; }
            set { SetProperty(ref _permission, value); }
        }


        private PrivateReasonType? _PrivateReason;
        public PrivateReasonType? PrivateReason
        {
            get { return _PrivateReason; }
            set { SetProperty(ref _PrivateReason, value); }
        }


        private double _LastWatchedPositionInterpolation;
        public double LastWatchedPositionInterpolation
        {
            get { return _LastWatchedPositionInterpolation; }
            set { SetProperty(ref _LastWatchedPositionInterpolation, value); }
        }


        #region 

        private bool _HasCacheProgress;
        public bool HasCacheProgress
        {
            get { return _HasCacheProgress; }
            set { SetProperty(ref _HasCacheProgress, value); }
        }

        private double _DownloadProgress;
        public double DownloadProgress
        {
            get { return _DownloadProgress; }
            set { SetProperty(ref _DownloadProgress, value); }
        }

        private bool _IsProgressUnknown;
        public bool IsProgressUnknown
        {
            get { return _IsProgressUnknown; }
            set { SetProperty(ref _IsProgressUnknown, value); }
        }

        private NicoVideoQuality? _CacheRequestedQuality;
        public NicoVideoQuality? CacheRequestedQuality
        {
            get { return _CacheRequestedQuality; }
            set { SetProperty(ref _CacheRequestedQuality, value); }
        }

        private NicoVideoQuality? _CacheProgressQuality;
        public NicoVideoQuality? CacheProgressQuality
        {
            get { return _CacheProgressQuality; }
            set { SetProperty(ref _CacheProgressQuality, value); }
        }

        private VideoCacheStatus? _CacheStatus;
        public VideoCacheStatus? CacheStatus
        {
            get { return _CacheStatus; }
            set { SetProperty(ref _CacheStatus, value); }
        }


        private void SubscribeCacheState(IVideoContent video)
        {
            UnsubscribeCacheState();

            if (video != null)
            {
                WeakReferenceMessenger.Default.Register<VideoCacheStatusChangedMessage, string>(this, RawVideoId);

                var cacheRequest = _cacheManager.GetVideoCache(video.Id);
                RefreshCacheRequestInfomation(cacheRequest?.Status, cacheRequest);
            }
        }

        void RefreshCacheRequestInfomation(VideoCacheStatus? cacheStatus, VideoCacheItem cacheItem = null)
        {
            _scheduler.Schedule(() =>
            {
                CacheStatus = cacheStatus;

                if (cacheStatus == null)
                {    
                    CacheRequestedQuality = null;
                    CacheProgressQuality = null;
                    DownloadProgress = 0;
                    HasCacheProgress = false;
                    IsProgressUnknown = false;
                }

                if (cacheStatus is VideoCacheStatus.Downloading)
                {
                    if (!WeakReferenceMessenger.Default.IsRegistered<VideoCacheProgressChangedMessage, string>(this, RawVideoId))
                    {
                        WeakReferenceMessenger.Default.Register<VideoCacheProgressChangedMessage, string>(this, RawVideoId);
                    }
                }
                else
                {
                    WeakReferenceMessenger.Default.Unregister<VideoCacheProgressChangedMessage>(this);
                }

                if (cacheItem != null)
                {
                    CacheRequestedQuality = cacheItem.RequestedVideoQuality.ToPlayVideoQuality();
                    CacheProgressQuality = cacheItem.DownloadedVideoQuality.ToPlayVideoQuality();
                    DownloadProgress = cacheItem.GetProgressNormalized();
                    HasCacheProgress = cacheStatus is VideoCacheStatus.Downloading or VideoCacheStatus.DownloadPaused;
                    IsProgressUnknown = HasCacheProgress && cacheItem.ProgressBytes is null or 0;
                }
            });
        }


        void IRecipient<VideoCacheStatusChangedMessage>.Receive(VideoCacheStatusChangedMessage message)
        {
            RefreshCacheRequestInfomation(message.Value.CacheStatus, message.Value.Item);
        }

        void IRecipient<VideoCacheProgressChangedMessage>.Receive(VideoCacheProgressChangedMessage message)
        {
            _scheduler.Schedule(() =>
            {
                var cacheItem = message.Value;
                DownloadProgress = cacheItem.GetProgressNormalized();
                HasCacheProgress = true;
                IsProgressUnknown = cacheItem.ProgressBytes is null or 0;
            });
        }
        

        private void UnsubscribeCacheState()
        {
            WeakReferenceMessenger.Default.Unregister<VideoCacheStatusChangedMessage, string>(this, RawVideoId);
            WeakReferenceMessenger.Default.Unregister<VideoCacheProgressChangedMessage, string>(this, RawVideoId);
        }

        #endregion

        void IRecipient<VideoPlayedMessage>.Receive(VideoPlayedMessage message)
        {
            Watched(message.Value);
        }

        void Watched(VideoPlayedMessage.VideoPlayedEventArgs args)
        {
            if (Data is IVideoContent video
                && video.Id == args.ContentId
                )
            {
                IsWatched = true;
                UnsubscriptionWatched();
                LastWatchedPositionInterpolation = Math.Clamp(args.PlayedPosition.TotalSeconds / video.Length.TotalSeconds, 0.0, 1.0);
            }
        }

        void SubscriptionWatchedIfNotWatch(IVideoContent video)
        {
            UnsubscriptionWatched();

            if (video != null)
            {
                var watched = _videoPlayedHistoryRepository.IsVideoPlayed(video.Id, out var hisotory);
                IsWatched = watched;
                if (!watched)
                {
                    StrongReferenceMessenger.Default.Register<VideoPlayedMessage, string>(this, video.Id);
                }
                else
                {
                    LastWatchedPositionInterpolation = hisotory.LastPlayedPosition != TimeSpan.Zero 
                        ? Math.Clamp(hisotory.LastPlayedPosition.TotalSeconds / video.Length.TotalSeconds, 0.0, 1.0)
                        : 1.0
                        ;
                }
            }
        }

        void UnsubscriptionWatched()
        {
            StrongReferenceMessenger.Default.Unregister<VideoPlayedMessage, string>(this, RawVideoId);
        }


        public async ValueTask InitializeAsync(CancellationToken ct)
        {
            if (Data?.Title != null)
            {
                SetTitle(Data.Title);
            }

            if (Data?.Title == null || Data?.ProviderId == null)
            {
                var data = await _nicoVideoProvider.GetNicoVideoInfo(RawVideoId, Data?.ProviderId == null);

                ct.ThrowIfCancellationRequested();

                Data = data;
            }

            if (Data != null)
            {
                 Setup(Data);
            }

            IsInitialized = true;
        }


        public void Setup(NicoVideo info)
        {
//            Debug.WriteLine("thumbnail reflect : " + info.RawVideoId);
            
            Label = info.Title;
            PostedAt = info.PostedAt;
            Length = info.Length;
            ViewCount = info.ViewCount;
            MylistCount = info.MylistCount;
            CommentCount = info.CommentCount;
            ThumbnailUrl ??= info.ThumbnailUrl;
            IsDeleted = info.IsDeleted;
            PrivateReason = Data.PrivateReasonType;
            Description = Data.Description;
            Permission = Data.Permission;

            if (info.Owner != null)
            {
                ProviderId = info.Owner.OwnerId;
                ProviderName = info.Owner.ScreenName;
                ProviderType = info.Owner.UserType;
            }

            SubscriptionWatchedIfNotWatch(info);
            UpdateIsHidenVideoOwner(info);
            SubscribeCacheState(info);
        }

        internal void SetDescription(int viewcount, int commentCount, int mylistCount)
        {
            ViewCount = viewcount;
            CommentCount = commentCount;
            MylistCount = mylistCount;
        }

        internal void SetTitle(string title)
        {
            Label = title;
        }
        internal void SetSubmitDate(DateTime submitDate)
        {
            PostedAt = submitDate;
        }

        internal void SetVideoDuration(TimeSpan duration)
        {
            Length = duration;
        }

        internal void SetThumbnailImage(string thumbnailImage)
        {
            ThumbnailUrl = thumbnailImage;
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

            IsInitialized = true;
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

            IsInitialized = true;
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
            ProviderId = data.Video.UserId ?? data.Video.CommunityId;
            ProviderType = data.Thread.GroupType == "channel" ? NicoVideoUserType.Channel : NicoVideoUserType.User;

            IsInitialized = true;
        }

    }






    [Flags]
    public enum VideoStatus
    {
        Watched = 0x0001,
        Filtered = 0x1000,
    }
}
