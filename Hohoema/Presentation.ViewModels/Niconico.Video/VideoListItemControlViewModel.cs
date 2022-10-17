using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser;
using Hohoema.Models.Domain.Player.Video.Cache;
using Hohoema.Models.Domain.VideoCache;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Models.UseCase.VideoCache.Events;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.Pages.VideoListPage.Commands;
using CommunityToolkit.Mvvm.Messaging;
using NiconicoToolkit;
using NiconicoToolkit.Video;
using CommunityToolkit.Mvvm.Input;
using Reactive.Bindings.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Hohoema.Models.UseCase.Niconico.Player.Events;
using Hohoema.Models.Domain.Playlist;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Hohoema.Presentation.ViewModels.VideoListPage
{
    public interface ISourcePlaylistPresenter
    {
        PlaylistId GetPlaylistId();
    }

    public class VideoItemViewModel : ObservableObject, IVideoContent, IPlaylistItemPlayable, IDisposable, ISourcePlaylistPresenter,
        IRecipient<VideoPlayedMessage>,
        IRecipient<PlaylistItemAddedMessage>,
        IRecipient<PlaylistItemRemovedMessage>,
        IRecipient<ItemIndexUpdatedMessage>,
        IRecipient<VideoCacheStatusChangedMessage>
    {
        private static readonly VideoPlayedHistoryRepository _videoPlayedHistoryRepository;
        private static readonly VideoCacheManager _cacheManager;
        private static readonly QueuePlaylist _queuePlaylist;
        private static readonly IMessenger _messenger;
        protected static readonly IScheduler _scheduler;

        static VideoItemViewModel()
        {
            _messenger = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<IMessenger>();
            _queuePlaylist = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<QueuePlaylist>();
            _cacheManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<VideoCacheManager>();
            _scheduler = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<IScheduler>();
            _videoPlayedHistoryRepository = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<VideoPlayedHistoryRepository>();
            _addWatchAfterCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<QueueAddItemCommand>();
            _removeWatchAfterCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<QueueRemoveItemCommand>();
        }

        protected void SetLength(TimeSpan length)
        {
            Length = length;
            OnPropertyChanged(nameof(Length));
        }


        public VideoId VideoId { get; init; }

        public TimeSpan Length { get; private set; }

        public string ThumbnailUrl { get; }

        public Uri ThumbnailUri => !string.IsNullOrWhiteSpace(ThumbnailUrl) ? new Uri(ThumbnailUrl) : null;

        public string Title { get; }
        
        public string Label => Title;

        public DateTime PostedAt { get; }

        public PlaylistItemToken? PlaylistItemToken { get; init; }

        bool IEquatable<IVideoContent>.Equals(IVideoContent other)
        {
            return this.VideoId == other.VideoId;
        }

        public VideoItemViewModel(
            VideoId videoId, string title, string thumbnailUrl, TimeSpan videoLength, DateTime postedAt
            )
        {
            VideoId = videoId;
            Title = title;
            ThumbnailUrl = thumbnailUrl;
            Length = videoLength;
            PostedAt = postedAt;

            SubscribeAll(videoId);
        }

        VideoId? _subsribedVideoId;
        protected void SubscribeAll(VideoId videoId)
        {
            if (_subsribedVideoId is not null and VideoId subscribedVideoId)
            {
                _messenger.Unregister<VideoPlayedMessage, VideoId>(this, subscribedVideoId);
                _messenger.Unregister<PlaylistItemAddedMessage, VideoId>(this, subscribedVideoId);
                _messenger.Unregister<PlaylistItemRemovedMessage, VideoId>(this, subscribedVideoId);
                _messenger.Unregister<ItemIndexUpdatedMessage, VideoId>(this, subscribedVideoId);
                _messenger.Unregister<VideoCacheStatusChangedMessage, VideoId>(this, subscribedVideoId);
                _subsribedVideoId = null;
            }

            _messenger.Register<VideoPlayedMessage, VideoId>(this, videoId);
            _messenger.Register<PlaylistItemAddedMessage, VideoId>(this, videoId);
            _messenger.Register<PlaylistItemRemovedMessage, VideoId>(this, videoId);
            _messenger.Register<ItemIndexUpdatedMessage, VideoId>(this, videoId);
            _messenger.Register<VideoCacheStatusChangedMessage, VideoId>(this, videoId);

            IsQueueItem = _queuePlaylist.Contains(videoId);
            if (IsQueueItem)
            {
                QueueItemIndex = _queuePlaylist.IndexOf(videoId);
            }
            else
            {
                QueueItemIndex = -1;
            }

            var cacheRequest = _cacheManager.GetVideoCache(videoId);
            RefreshCacheStatus(cacheRequest?.Status, cacheRequest);
            SubscriptionWatchedIfNotWatch(videoId);

            _subsribedVideoId = videoId;
        }

        public virtual void Dispose()
        {
            if (_subsribedVideoId is not null and VideoId subscribedVideoId)
            {
                _messenger.Unregister<VideoPlayedMessage, VideoId>(this, subscribedVideoId);
                _messenger.Unregister<PlaylistItemAddedMessage, VideoId>(this, subscribedVideoId);
                _messenger.Unregister<PlaylistItemRemovedMessage, VideoId>(this, subscribedVideoId);
                _messenger.Unregister<ItemIndexUpdatedMessage, VideoId>(this, subscribedVideoId);
                _messenger.Unregister<VideoCacheStatusChangedMessage, VideoId>(this, subscribedVideoId);
                _subsribedVideoId = null;
            }
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
            LastWatchedPositionInterpolation = Math.Clamp(args.PlayedPosition.TotalSeconds / Length.TotalSeconds, 0.0, 1.0);
        }

        void SubscriptionWatchedIfNotWatch(VideoId videoId)
        {
            UnsubscriptionWatched(_subsribedVideoId);

            var watched = _videoPlayedHistoryRepository.IsVideoPlayed(videoId, out var hisotory);
            IsWatched = watched;
            if (!watched)
            {
                if (!_messenger.IsRegistered<VideoPlayedMessage, VideoId>(this, videoId))
                {
                    _messenger.Register<VideoPlayedMessage, VideoId>(this, videoId);
                }
            }
            else
            {
                LastWatchedPositionInterpolation = hisotory.LastPlayedPosition != TimeSpan.Zero
                    ? Math.Clamp(hisotory.LastPlayedPosition.TotalSeconds / Length.TotalSeconds, 0.0, 1.0)
                    : 1.0
                    ;
            }
        }

        void UnsubscriptionWatched(VideoId? videoId)
        {
            if (videoId != null)
            {
                _messenger.Unregister<VideoPlayedMessage, VideoId>(this, videoId.Value);
            }
        }




        #endregion

        #region Queue Item

        public PlaylistId GetPlaylistId()
        {
            return PlaylistItemToken?.Playlist.PlaylistId;
        }


        private static readonly QueueAddItemCommand _addWatchAfterCommand;
        public QueueAddItemCommand AddWatchAfterCommand => _addWatchAfterCommand;

        private static readonly QueueRemoveItemCommand _removeWatchAfterCommand;
        public QueueRemoveItemCommand RemoveWatchAfterCommand => _removeWatchAfterCommand;


        private RelayCommand<object> _toggleWatchAfterCommand;
        public RelayCommand<object> ToggleWatchAfterCommand => _toggleWatchAfterCommand
            ??= new RelayCommand<object>(parameter => 
            {
                if (IsQueueItem)
                {
                    (_removeWatchAfterCommand as ICommand).Execute(parameter);
                }
                else
                {
                    (_addWatchAfterCommand as ICommand).Execute(parameter);
                }
            });


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

        void IRecipient<PlaylistItemAddedMessage>.Receive(PlaylistItemAddedMessage message)
        {
            if (message.Value.PlaylistId != QueuePlaylist.Id) { return; }
            _scheduler.Schedule(() => IsQueueItem = true);

        }

        void IRecipient<PlaylistItemRemovedMessage>.Receive(PlaylistItemRemovedMessage message)
        {
            if (message.Value.PlaylistId != QueuePlaylist.Id) { return; }
            _scheduler.Schedule(() =>
            {
                IsQueueItem = false;
                QueueItemIndex = -1;
            });
        }


        void IRecipient<ItemIndexUpdatedMessage>.Receive(ItemIndexUpdatedMessage message)
        {
            if (message.Value.PlaylistId != QueuePlaylist.Id) { return; }
            _scheduler.Schedule(() =>
            {
                QueueItemIndex = message.Value.Index;
            });
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
                RefreshCacheStatus(message.Value.CacheStatus, message.Value.Item);
            });
        }

        void RefreshCacheStatus(VideoCacheStatus? status, VideoCacheItem item)
        {
            CacheStatus = status;
            if (item?.DownloadedVideoQuality is not null and not NicoVideoQuality.Unknown and var quality)
            {
                CacheRequestedQuality = quality;
            }
            else
            {
                CacheRequestedQuality = item?.RequestedVideoQuality;
            }
        }

        private void UnsubscribeCacheState()
        {
            _messenger.Unregister<VideoCacheStatusChangedMessage, VideoId>(this, VideoId);
        }

        #endregion


        
    }






    public class VideoListItemControlViewModel : VideoItemViewModel, IVideoDetail, IDisposable,
        IRecipient<VideoOwnerFilteringAddedMessage>,
        IRecipient<VideoOwnerFilteringRemovedMessage>
    {
        static VideoListItemControlViewModel()
        {
            _nicoVideoProvider = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<NicoVideoProvider>();
            _ngSettings = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<VideoFilteringSettings>();
            _openVideoOwnerPageCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<OpenVideoOwnerPageCommand>();
            _messenger = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<IMessenger>();
        }

        public VideoListItemControlViewModel(IVideoContent video)
            : this(video.VideoId, video.Title, video.ThumbnailUrl, video.Length, video.PostedAt)
        {

        }

        public VideoListItemControlViewModel(
            VideoId videoId, string title, string thumbnailUrl, TimeSpan videoLength, DateTime postedAt
            )
            : base(videoId, title, thumbnailUrl, videoLength, postedAt)
        {
            UpdateIsHidenVideoOwner(this);
            
            if (VideoId != VideoId && VideoId != null)
            {
                SubscribeAll(VideoId);
            }
        }

        public VideoListItemControlViewModel(
            NvapiVideoItem videoItem
            )
            : this(videoItem.Id, videoItem.Title, videoItem.Thumbnail.Url.OriginalString, TimeSpan.FromSeconds(videoItem.Duration), videoItem.RegisteredAt.DateTime)
        {
            ViewCount = videoItem.Count.View;
            CommentCount = videoItem.Count.Comment;
            MylistCount = videoItem.Count.Mylist;

            IsDeleted = videoItem.IsDeleted;


            if (videoItem.Owner is not null)
            {
                _ProviderId = videoItem.Owner.Id;
                ProviderType = videoItem.Owner.OwnerType;
                _ProviderName = videoItem.Owner.Name;
                ProviderIconUrl = videoItem.Owner.IconUrl?.OriginalString;
            }

            UpdateIsHidenVideoOwner(this);

            if (VideoId != VideoId && VideoId != null)
            {
                SubscribeAll(VideoId);
            }
        }

        public VideoListItemControlViewModel(NiconicoToolkit.SearchWithCeApi.Video.VideoItem video, NiconicoToolkit.SearchWithCeApi.Video.ThreadItem thread)            
            : this(video.Id, video.Title, video.ThumbnailUrl.OriginalString, TimeSpan.FromSeconds(video.LengthInSeconds), video.FirstRetrieve.DateTime)
        {
            ViewCount = video.ViewCount;
            MylistCount = video.MylistCount;
            CommentCount =thread.NumRes;
            _IsDeleted = video.Deleted != 0;
            if (_IsDeleted && Enum.IsDefined(typeof(PrivateReasonType), video.Deleted))
            {
                _PrivateReason = (PrivateReasonType)video.Deleted;
            }
            
            _Description = video.Description;
            
            if (video.ProviderType == NiconicoToolkit.SearchWithCeApi.Video.VideoProviderType.Channel)
            {
                _ProviderId = video.CommunityId;
                //_ProviderName = video.name;
                ProviderType = OwnerType.Channel;
                RegisterVideoOwnerFilteringMessageReceiver(_ProviderId, null);
            }
            else if (video.ProviderType == NiconicoToolkit.SearchWithCeApi.Video.VideoProviderType.Regular)
            {
                _ProviderId = video.UserId.ToString();
                ProviderType = OwnerType.User;
                RegisterVideoOwnerFilteringMessageReceiver(_ProviderId, null);
            }

            UpdateIsHidenVideoOwner(this);

            if (VideoId != VideoId && VideoId != null)
            {
                SubscribeAll(VideoId);
            }
        }

        public VideoListItemControlViewModel(
           NicoVideo videoItem
           )
           : this(videoItem.Id, videoItem.Title, videoItem.ThumbnailUrl, videoItem.Length, videoItem.PostedAt)
        {
            if (videoItem.Owner is not null)
            {
                _ProviderId = videoItem.Owner.OwnerId;
                ProviderType = videoItem.Owner.UserType;
                _ProviderName = videoItem.Owner.ScreenName;
                ProviderIconUrl = videoItem.Owner.IconUrl;
            }

            UpdateIsHidenVideoOwner(this);

            if (VideoId != VideoId && VideoId != null)
            {
                SubscribeAll(VideoId);
            }
        }

        public VideoListItemControlViewModel(
           IVideoDetail videoItem
           )
           : this(videoItem.VideoId, videoItem.Title, videoItem.ThumbnailUrl, videoItem.Length, videoItem.PostedAt)
        {
            _ProviderId = videoItem.ProviderId;
            ProviderType = videoItem.ProviderType;

            ViewCount = videoItem.ViewCount;
            CommentCount = videoItem.CommentCount;
            MylistCount = videoItem.MylistCount;

            UpdateIsHidenVideoOwner(this);

            if (VideoId != VideoId && VideoId != null)
            {
                SubscribeAll(VideoId);
            }
        }

        public bool Equals(IVideoContent other)
        {
            return VideoId == other.VideoId;
        }

        public override int GetHashCode()
        {
            return VideoId.GetHashCode();
        }


        protected static readonly NicoVideoProvider _nicoVideoProvider;
        private static readonly VideoFilteringSettings _ngSettings;

        private static readonly OpenVideoOwnerPageCommand _openVideoOwnerPageCommand;
        private static readonly IMessenger _messenger;

        public OpenVideoOwnerPageCommand OpenVideoOwnerPageCommand => _openVideoOwnerPageCommand;


        private string _ProviderId;
        public string ProviderId
        {
            get => _ProviderId;
            set
            {
                var oldProviderId = _ProviderId;
                if (SetProperty(ref _ProviderId, value))
                {
                    RegisterVideoOwnerFilteringMessageReceiver(_ProviderId, oldProviderId);
                    UpdateIsHidenVideoOwner(this);
                }
            }
        }
        
        private string _ProviderName;
        public string ProviderName
        {
            get { return _ProviderName; }
            set { SetProperty(ref _ProviderName, value); }
        }


        
        public string ProviderIconUrl { get; private set; }

        public OwnerType ProviderType { get; set; }

        public IMylist OnwerPlaylist { get; }

        public VideoStatus VideoStatus { get; private set; }

        private string _Description;
        public string Description
        {
            get { return _Description; }
            set { SetProperty(ref _Description, value); }
        }


        public int ViewCount { get; set; }


        public int MylistCount { get; set; }

        public int CommentCount { get; set; }

        private bool _IsDeleted;
        public bool IsDeleted
        {
            get { return _IsDeleted; }
            set { SetProperty(ref _IsDeleted, value); }
        }

        private bool _IsSensitiveContent;
        public bool IsSensitiveContent
        {
            get { return _IsSensitiveContent; }
            set { SetProperty(ref _IsSensitiveContent, value); }
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

        void RegisterVideoOwnerFilteringMessageReceiver(string currentProviderId, string oldProviderId)
        {
            if (oldProviderId is not null)
            {
                _messenger.Unregister<VideoOwnerFilteringAddedMessage, string>(this, oldProviderId);
                _messenger.Unregister<VideoOwnerFilteringRemovedMessage, string>(this, oldProviderId);
            }

            if (currentProviderId is not null)
            {
                _messenger.Register<VideoOwnerFilteringAddedMessage, string>(this, currentProviderId);
                _messenger.Register<VideoOwnerFilteringRemovedMessage, string>(this, currentProviderId);
            }
        }

        void IRecipient<VideoOwnerFilteringAddedMessage>.Receive(VideoOwnerFilteringAddedMessage message)
        {
            UpdateIsHidenVideoOwner(this);

        }

        void IRecipient<VideoOwnerFilteringRemovedMessage>.Receive(VideoOwnerFilteringRemovedMessage message)
        {
            UpdateIsHidenVideoOwner(this);
        }


        private FilteredResult _VideoHiddenInfo;
        public FilteredResult VideoHiddenInfo
        {
            get { return _VideoHiddenInfo; }
            set { SetProperty(ref _VideoHiddenInfo, value); }
        }


        protected void UpdateIsHidenVideoOwner(IVideoContent video)
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

        private RelayCommand _UnregistrationHiddenVideoOwnerCommand;
        public RelayCommand UnregistrationHiddenVideoOwnerCommand =>
            _UnregistrationHiddenVideoOwnerCommand ?? (_UnregistrationHiddenVideoOwnerCommand = new RelayCommand(ExecuteUnregistrationHiddenVideoOwnerCommand));

        void ExecuteUnregistrationHiddenVideoOwnerCommand()
        {
            if (ProviderId != null)
            {
                _ngSettings.RemoveHiddenVideoOwnerId(ProviderId);
            }

        }



        #endregion



        public override void Dispose()
        {
            base.Dispose();

            RegisterVideoOwnerFilteringMessageReceiver(null, _ProviderId);
        }




        public async ValueTask EnsureProviderIdAsync(CancellationToken ct)
        {
            if (string.IsNullOrEmpty(Label))
            {
                Debug.WriteLine("");
                return;
            }

            if (ProviderId is null && ProviderType != OwnerType.Hidden)
            {
                var owner = await _nicoVideoProvider.ResolveVideoOwnerAsync(VideoId);
                if (owner is not null)
                {
                    ProviderId = owner.OwnerId;
                    ProviderType = owner.UserType;
                    ProviderName = owner.ScreenName;
                }
            }

            UpdateIsHidenVideoOwner(this);

            OnInitialized();
        }


        protected virtual void OnInitialized() { }

        protected virtual VideoPlayPayload MakeVideoPlayPayload()
		{
			return new VideoPlayPayload()
			{
				VideoId = VideoId,
				Quality = null,
			};
		}
    }

    public static class VideoListItemControlViewModelExtesnsion
    {


       

    }




    [Flags]
    public enum VideoStatus
    {
        Watched = 0x0001,
        Filtered = 0x1000,
    }
}
