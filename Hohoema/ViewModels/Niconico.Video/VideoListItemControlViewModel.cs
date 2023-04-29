#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using Hohoema.Models.VideoCache;
using Hohoema.Contracts.Player;
using Hohoema.Services.VideoCache.Events;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.Pages.VideoListPage.Commands;
using NiconicoToolkit.Video;
using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using NiconicoToolkit.Live.WatchPageProp;

namespace Hohoema.ViewModels.VideoListPage;

public interface ISourcePlaylistPresenter
{
    PlaylistId GetPlaylistId();
}

public partial class VideoItemViewModel 
    : ObservableObject
    , IVideoContent
    , IPlaylistItemPlayable
    , ISourcePlaylistPresenter
{
    private static readonly VideoWatchedRepository _videoWatchedRepository;
    private static readonly VideoCacheManager _cacheManager;
    private static readonly QueuePlaylist _queuePlaylist;
    private static readonly IMessenger _messenger;
    protected static readonly IScheduler _scheduler;

    static VideoItemViewModel()
    {
        _messenger = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<IMessenger>();
        _queuePlaylist = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<QueuePlaylist>();
        _cacheManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<VideoCacheManager>();
        _scheduler = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<IScheduler>();
        _videoWatchedRepository = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<VideoWatchedRepository>();
        _addWatchAfterCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<QueueAddItemCommand>();
        _removeWatchAfterCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<QueueRemoveItemCommand>();
    }


    public VideoId VideoId { get; init; }

    public TimeSpan Length { get; init; }

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

        Initialize(VideoId);
    }

    void Initialize(VideoId videoId)
    {
        RefreshCacheStatus(_cacheManager.GetVideoCache(videoId));
        InitializeWatched(videoId);
        InitializeQueueItem(videoId);
    }


    void InitializeQueueItem(VideoId videoId)
    {
        IsQueueItem = _queuePlaylist.Contains(videoId);
        if (IsQueueItem)
        {
            QueueItemIndex = _queuePlaylist.IndexOf(videoId);
        }
        else
        {
            QueueItemIndex = -1;
        }
    }

    #region Watched

    private bool _IsWatched;
    public bool IsWatched
    {
        get { return _IsWatched; }
        private set { SetProperty(ref _IsWatched, value); }
    }

    private double _LastWatchedPositionInterpolation;
    public double LastWatchedPositionInterpolation
    {
        get { return _LastWatchedPositionInterpolation; }
        private set { SetProperty(ref _LastWatchedPositionInterpolation, value); }
    }

    public void OnWatched(VideoWatchedMessage message)
    {
        if (message.Value.ContentId == VideoId)
        {
            IsWatched = true;
            LastWatchedPositionInterpolation = Math.Clamp(message.Value.PlayedPosition.TotalSeconds / Length.TotalSeconds, 0.0, 1.0);
        }
    }

    void InitializeWatched(VideoId videoId)
    {
        var watched = _videoWatchedRepository.IsVideoPlayed(videoId, out var hisotory);
        IsWatched = watched;
        if (watched)
        {
            LastWatchedPositionInterpolation = hisotory.LastPlayedPosition != TimeSpan.Zero
                ? Math.Clamp(hisotory.LastPlayedPosition.TotalSeconds / Length.TotalSeconds, 0.0, 1.0)
                : 1.0
                ;
        }
        else
        {
            LastWatchedPositionInterpolation = 0.0;
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

    private static readonly RelayCommand<IVideoContent> _playVideoCommand = new ((IVideoContent? video) => 
    {
        _messenger.Send(VideoPlayRequestMessage.PlayVideo(video!.VideoId));
    });

    public RelayCommand<IVideoContent> PlayVideoCommand => _playVideoCommand;

    [RelayCommand]
    public void ToggleWatchAfter(object parameter)
    {
        if (IsQueueItem)
        {
            (_removeWatchAfterCommand as ICommand).Execute(parameter);
        }
        else
        {
            (_addWatchAfterCommand as ICommand).Execute(parameter);
        }
    }


    private bool _IsQueueItem;
    public bool IsQueueItem
    {
        get { return _IsQueueItem; }
        private set { SetProperty(ref _IsQueueItem, value); }
    }

    private int _QueueItemIndex;
    public int QueueItemIndex
    {
        get { return _QueueItemIndex; }
        private set { SetProperty(ref _QueueItemIndex, value + 1); }
    }



    public void OnPlaylistItemAdded(PlaylistItemAddedMessage message)
    {
        if (!message.Value.AddedItems.Any(x => x.VideoId == VideoId)) { return; }
        if (message.Value.PlaylistId != QueuePlaylist.Id) { return; }
        _scheduler.Schedule(() => IsQueueItem = true);
    }

    public void OnPlaylistItemRemoved(PlaylistItemRemovedMessage message)
    {
        if (!message.Value.RemovedItems.Any(x => x.VideoId == VideoId)) { return; }
        if (message.Value.PlaylistId != QueuePlaylist.Id) { return; }
        _scheduler.Schedule(() =>
        {
            IsQueueItem = false;
            QueueItemIndex = -1;
        });
    }


    public void OnQueueItemIndexUpdated(ItemIndexUpdatedMessage message)
    {
        if (message.Value.ContentId != VideoId) { return; }
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
        private set { SetProperty(ref _CacheRequestedQuality, value); }
    }

    private VideoCacheStatus? _CacheStatus;
    public VideoCacheStatus? CacheStatus
    {
        get { return _CacheStatus; }
        private set { SetProperty(ref _CacheStatus, value); }
    }

    public void OnCacheStatusChanged(VideoCacheStatusChangedMessage message)
    {
        if (message.Value.VideoId == VideoId)
        {
            RefreshCacheStatus(message.Value.Item);
        }
    }

    void RefreshCacheStatus(VideoCacheItem? item)
    {
        if (item == null)
        {
            CacheStatus = null;
            CacheRequestedQuality = null;
        }
        else
        {
            CacheStatus = item.Status;
            if (item.DownloadedVideoQuality is not NicoVideoQuality.Unknown and var quality)
            {
                CacheRequestedQuality = quality;
            }
            else
            {
                CacheRequestedQuality = item.RequestedVideoQuality;
            }
        }
    }


    #endregion


    
}






public class VideoListItemControlViewModel
    : VideoItemViewModel
    , IVideoDetail
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
        InitializeIsHiddenVideoOwner(this);
    }

    public VideoListItemControlViewModel(
        NvapiVideoItem videoItem
        )
        : this(videoItem.Id, videoItem.Title, videoItem.Thumbnail.Url.OriginalString, TimeSpan.FromSeconds(videoItem.Duration), videoItem.RegisteredAt.DateTime)
    {
        ViewCount = videoItem.Count.View;
        CommentCount = videoItem.Count.Comment;
        MylistCount = videoItem.Count.Mylist;

        _IsDeleted = videoItem.IsDeleted;
        _IsSensitiveContent = videoItem.RequireSensitiveMasking;

        if (videoItem.Owner is not null)
        {
            _ProviderId = videoItem.Owner.Id;
            ProviderType = videoItem.Owner.OwnerType;
            _ProviderName = videoItem.Owner.Name;
            ProviderIconUrl = videoItem.Owner.IconUrl?.OriginalString;
        }

        IsRequirePayment = videoItem.IsPaymentRequired;

        InitializeIsHiddenVideoOwner(this);
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

        InitializeIsHiddenVideoOwner(this);
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

        InitializeIsHiddenVideoOwner(this);
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
                InitializeIsHiddenVideoOwner(this);
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

    public OwnerType ProviderType { get; init; }

    private string _Description;
    public string Description
    {
        get { return _Description; }
        set { SetProperty(ref _Description, value); }
    }


    public int ViewCount { get; init; }
    public int MylistCount { get; init; }
    public int CommentCount { get; init; }

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

    public bool IsRequirePayment { get; }

    #region NG 

    public void OnVideoOwnerFilteringAdded(VideoOwnerFilteringAddedMessage message)
    {
        if (message.Value.OwnerId == ProviderId
            && _ngSettings.TryGetHiddenReason(this, out var result)
            )
        {
            VideoHiddenInfo = result;
        }
    }

    public void OnVideoOwnerFilteringRemoved(VideoOwnerFilteringRemovedMessage message)
    {
        if (message.Value.OwnerId == ProviderId)
        {
            VideoHiddenInfo = null;
        }
    }


    private FilteredResult? _VideoHiddenInfo;
    public FilteredResult? VideoHiddenInfo
    {
        get { return _VideoHiddenInfo; }
        private set { SetProperty(ref _VideoHiddenInfo, value); }
    }

    void InitializeIsHiddenVideoOwner(IVideoContent video)
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

    protected virtual VideoPlayPayload MakeVideoPlayPayload()
    {
        return new VideoPlayPayload()
        {
            VideoId = VideoId,
            Quality = null,
        };
    }
}

