using Hohoema.Models.Domain;
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
    public class VideoItemViewModel : FixPrism.BindableBase, IVideoContent, IDisposable,
        IRecipient<VideoPlayedMessage>,
        IRecipient<QueueItemAddedMessage>,
        IRecipient<QueueItemRemovedMessage>,
        IRecipient<QueueItemIndexUpdateMessage>,
        IRecipient<VideoCacheStatusChangedMessage>
    {
        private static readonly NicoVideoProvider _nicoVideoProvider;
        private static readonly HohoemaPlaylist _hohoemaPlaylist;
        private static readonly NicoVideoCacheRepository _nicoVideoRepository;
        private static readonly VideoPlayedHistoryRepository _videoPlayedHistoryRepository;
        private static readonly UserNameProvider _userNameProvider;
        private static readonly ChannelProvider _channelProvider;
        private static readonly VideoFilteringSettings _ngSettings;
        private static readonly VideoCacheManager _cacheManager;
        protected static readonly IScheduler _scheduler;

        static VideoItemViewModel()
        {
            _nicoVideoProvider = App.Current.Container.Resolve<NicoVideoProvider>();
            _hohoemaPlaylist = App.Current.Container.Resolve<HohoemaPlaylist>();
            _ngSettings = App.Current.Container.Resolve<VideoFilteringSettings>();
            _cacheManager = App.Current.Container.Resolve<VideoCacheManager>();
            _scheduler = App.Current.Container.Resolve<IScheduler>();
            _nicoVideoRepository = App.Current.Container.Resolve<NicoVideoCacheRepository>();
            _videoPlayedHistoryRepository = App.Current.Container.Resolve<VideoPlayedHistoryRepository>();
            _userNameProvider = App.Current.Container.Resolve<UserNameProvider>();
            _channelProvider = App.Current.Container.Resolve<ChannelProvider>();
            _addWatchAfterCommand = App.Current.Container.Resolve<QueueAddItemCommand>();

            _removeWatchAfterCommand = App.Current.Container.Resolve<QueueRemoveItemCommand>();
        }

        public string RawVideoId { get; }

        public TimeSpan Length { get; }

        public string ThumbnailUrl { get; }

        public string Title { get; }

        public string Id => RawVideoId;

        public string Label => Title;

        bool IEquatable<IVideoContent>.Equals(IVideoContent other)
        {
            return this.RawVideoId == other.Id;
        }

        public VideoItemViewModel(
            string rawVideoId, string title, string thumbnailUrl, TimeSpan videoLength
            )
        {
            RawVideoId = rawVideoId;
            Title = title;
            ThumbnailUrl = thumbnailUrl;
            Length = videoLength;

            WeakReferenceMessenger.Default.Register<VideoPlayedMessage, string>(this, RawVideoId);
            WeakReferenceMessenger.Default.Register<QueueItemAddedMessage, string>(this, RawVideoId);
            WeakReferenceMessenger.Default.Register<QueueItemRemovedMessage, string>(this, RawVideoId);
            WeakReferenceMessenger.Default.Register<QueueItemIndexUpdateMessage, string>(this, RawVideoId);

            (IsQueueItem, QueueItemIndex) = _hohoemaPlaylist.IsQueuePlaylistItem(RawVideoId);

            WeakReferenceMessenger.Default.Register<VideoCacheStatusChangedMessage, string>(this, RawVideoId);

            var cacheRequest = _cacheManager.GetVideoCache(RawVideoId);
            CacheStatus = cacheRequest?.Status;
            CacheRequestedQuality = cacheRequest?.RequestedVideoQuality;
        }


        public override void Dispose()
        {
            base.Dispose();

            WeakReferenceMessenger.Default.UnregisterAll(this, RawVideoId);
        }

        #region Watched

        private bool _IsWatched;
        public bool IsWatched
        {
            get { return _IsWatched; }
            set { SetProperty(ref _IsWatched, value); }
        }

        private double _LastWatchedPositionInterpolation;
        public double LastWatchedPositionInterpolation
        {
            get { return _LastWatchedPositionInterpolation; }
            set { SetProperty(ref _LastWatchedPositionInterpolation, value); }
        }


        void IRecipient<VideoPlayedMessage>.Receive(VideoPlayedMessage message)
        {
            Watched(message.Value);
        }

        void Watched(VideoPlayedMessage.VideoPlayedEventArgs args)
        {
            IsWatched = true;
            UnsubscriptionWatched();
            LastWatchedPositionInterpolation = Math.Clamp(args.PlayedPosition.TotalSeconds / Length.TotalSeconds, 0.0, 1.0);
        }

        void SubscriptionWatchedIfNotWatch()
        {
            UnsubscriptionWatched();

            var watched = _videoPlayedHistoryRepository.IsVideoPlayed(RawVideoId, out var hisotory);
            IsWatched = watched;
            if (!watched)
            {
                StrongReferenceMessenger.Default.Register<VideoPlayedMessage, string>(this, RawVideoId);
            }
            else
            {
                LastWatchedPositionInterpolation = hisotory.LastPlayedPosition != TimeSpan.Zero
                    ? Math.Clamp(hisotory.LastPlayedPosition.TotalSeconds / Length.TotalSeconds, 0.0, 1.0)
                    : 1.0
                    ;
            }
        }

        void UnsubscriptionWatched()
        {
            StrongReferenceMessenger.Default.Unregister<VideoPlayedMessage, string>(this, RawVideoId);
        }




        #endregion

        #region Queue Item


        private static readonly QueueAddItemCommand _addWatchAfterCommand;
        public QueueAddItemCommand AddWatchAfterCommand => _addWatchAfterCommand;

        private static readonly QueueRemoveItemCommand _removeWatchAfterCommand;
        public QueueRemoveItemCommand RemoveWatchAfterCommand => _removeWatchAfterCommand;


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


        #endregion



        #region VideoCache

        private NicoVideoQuality? _CacheRequestedQuality;
        public NicoVideoQuality? CacheRequestedQuality
        {
            get { return _CacheRequestedQuality; }
            set { SetProperty(ref _CacheRequestedQuality, value); }
        }

        private VideoCacheStatus? _CacheStatus;
        public VideoCacheStatus? CacheStatus
        {
            get { return _CacheStatus; }
            set { SetProperty(ref _CacheStatus, value); }
        }

        void IRecipient<VideoCacheStatusChangedMessage>.Receive(VideoCacheStatusChangedMessage message)
        {
            _scheduler.Schedule(() =>
            {
                CacheStatus = message.Value.CacheStatus;
                CacheRequestedQuality = message.Value.Item?.RequestedVideoQuality;
            });
        }

        private void UnsubscribeCacheState()
        {
            WeakReferenceMessenger.Default.Unregister<VideoCacheStatusChangedMessage, string>(this, RawVideoId);
        }

        #endregion


        
    }






    public class VideoInfoControlViewModel : VideoItemViewModel, IVideoDetail, IDisposable 
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

            _openVideoOwnerPageCommand = App.Current.Container.Resolve<OpenVideoOwnerPageCommand>();
        }


        public VideoInfoControlViewModel(
            string rawVideoId, string title, string thumbnailUrl, TimeSpan videoLength
            )
            : base(rawVideoId, title, thumbnailUrl, videoLength)
        {
            _ngSettings.VideoOwnerFilterAdded += _ngSettings_VideoOwnerFilterAdded;
            _ngSettings.VideoOwnerFilterRemoved += _ngSettings_VideoOwnerFilterRemoved;

        }

        public VideoInfoControlViewModel(NicoVideo data)            
            : this(data.RawVideoId, data.Title, data.ThumbnailUrl, data.Length)
        {
            Data = data;
            
            _PostedAt = data.PostedAt;
            _ViewCount = data.ViewCount;
            _MylistCount = data.MylistCount;
            _CommentCount = data.CommentCount;
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

            UpdateIsHidenVideoOwner(data);
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

        private static readonly OpenVideoOwnerPageCommand _openVideoOwnerPageCommand;

        public OpenVideoOwnerPageCommand OpenVideoOwnerPageCommand => _openVideoOwnerPageCommand;


        public NicoVideo Data { get; private set; }

        public string ProviderId { get; set; }
        
        private string _ProviderName;
        public string ProviderName
        {
            get { return _ProviderName; }
            set { SetProperty(ref _ProviderName, value); }
        }


        public NicoVideoUserType ProviderType { get; set; }

        public IMylist OnwerPlaylist { get; }

        public VideoStatus VideoStatus { get; private set; }

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

        private bool _IsDeleted;
        public bool IsDeleted
        {
            get { return _IsDeleted; }
            set { SetProperty(ref _IsDeleted, value); }
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

        #region NG 




        private FilteredResult _VideoHiddenInfo;
        public FilteredResult VideoHiddenInfo
        {
            get { return _VideoHiddenInfo; }
            set { SetProperty(ref _VideoHiddenInfo, value); }
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



        #endregion



        public override void Dispose()
        {
            base.Dispose();

            _ngSettings.VideoOwnerFilterAdded -= _ngSettings_VideoOwnerFilterAdded;
            _ngSettings.VideoOwnerFilterRemoved -= _ngSettings_VideoOwnerFilterRemoved;
        }




        public async ValueTask InitializeAsync(CancellationToken ct)
        {
            if (Data?.Title == null || Data?.ProviderId == null)
            {
                var data = await _nicoVideoProvider.GetNicoVideoInfo(RawVideoId, Data?.ProviderId == null);

                ct.ThrowIfCancellationRequested();

                Data = data;
            }

            if (Data != null)
            {
                this.Setup(Data);
            }

            UpdateIsHidenVideoOwner(this);
        }


        protected virtual VideoPlayPayload MakeVideoPlayPayload()
		{
			return new VideoPlayPayload()
			{
				VideoId = RawVideoId,
				Quality = null,
			};
		}



    }

    public static class VideoInfoControlViewModelExtesnsion
    {


        public static void Setup(this VideoInfoControlViewModel vm, NicoVideo data)
        {
            vm.PostedAt = data.PostedAt;
            vm.ViewCount = data.ViewCount;
            vm.MylistCount = data.MylistCount;
            vm.CommentCount = data.CommentCount;
            vm.IsDeleted = data.IsDeleted;
            vm.PrivateReason = data.PrivateReasonType;
            vm.Description = data.Description;
            vm.Permission = data.Permission;

            if (data.Owner != null)
            {
                vm.ProviderId = data.Owner.OwnerId;
                vm.ProviderType = data.Owner.UserType;
            }
        }

        public static void Setup(this VideoInfoControlViewModel vm, Mntone.Nico2.Users.Video.VideoData data)
        {
            if (vm.RawVideoId != data.VideoId) { throw new Exception(); }

            vm.PostedAt = data.SubmitTime;
        }


        // とりあえずマイリストから取得したデータによる初期化
        public static void Setup(this VideoInfoControlViewModel vm, MylistData data)
        {
            if (data.WatchId != vm.RawVideoId) { throw new Exception(); }

            vm.PostedAt = data.CreateTime;

            vm.ViewCount = (int)data.ViewCount;
            vm.CommentCount = (int)data.CommentCount;
            vm.MylistCount = (int)data.MylistCount;
        }


        // 個別マイリストから取得したデータによる初期化
        public static void Setup(this VideoInfoControlViewModel vm, VideoInfo data)
        {
            if (vm.RawVideoId != data.Video.Id) { throw new Exception(); }

            vm.PostedAt = data.Video.UploadTime;
            vm.ViewCount = (int)data.Video.ViewCount;
            vm.CommentCount = (int)data.Thread.GetCommentCount();
            vm.MylistCount = (int)data.Video.MylistCount;

            vm.ProviderId = data.Video.UserId ?? data.Video.CommunityId;
            vm.ProviderType = data.Thread.GroupType == "channel" ? NicoVideoUserType.Channel : NicoVideoUserType.User;
        }
    }




    [Flags]
    public enum VideoStatus
    {
        Watched = 0x0001,
        Filtered = 0x1000,
    }
}
