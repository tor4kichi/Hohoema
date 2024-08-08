#nullable enable
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Playlist;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using Hohoema.Services;
using Hohoema.Services.VideoCache.Events;
using Hohoema.ViewModels.Niconico.Live;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.VideoListPage;
using I18NPortable;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Live;
using NiconicoToolkit.FollowingsActivity;
using NiconicoToolkit.Video;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using NiconicoToolkit;
using Hohoema.Models.Niconico;
using System.Diagnostics;

namespace Hohoema.ViewModels.Pages.Niconico.FollowingsActivity;

public sealed partial class FollowingsActivityPageViewModel 
    : HohoemaListingPageViewModelBase<IFollowingsActivityItem>
    , IRecipient<VideoWatchedMessage>
    , IRecipient<PlaylistItemAddedMessage>
    , IRecipient<PlaylistItemRemovedMessage>
    , IRecipient<ItemIndexUpdatedMessage>
    , IRecipient<VideoCacheStatusChangedMessage>
{
    private readonly IMessenger _messenger;
    private readonly NiconicoSession _niconicoSession;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly OpenLiveContentCommand _openLiveContentCommand;
    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public SubscriptionManager SubscriptionManager { get; }
    public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }


    bool _FollowingsActivityMuteContextTriggersChanged;

    public ImmutableArray<ActivityType> FollowingsActivityTypeList { get; } = new[]
    {
        ActivityType.Publish,
        ActivityType.Video,
        ActivityType.Live,
        ActivityType.All,
    }
    .ToImmutableArray();

    [ObservableProperty]
    ActivityType _selectedActivityType;

    partial void OnSelectedActivityTypeChanged(ActivityType value)
    {
        ResetList();
    }

    [ObservableProperty]
    NiconicoActivityActor? _selectedActor;

    partial void OnSelectedActorChanged(NiconicoActivityActor? value)
    {
        ResetList();
    }

    public FollowingsActivityPageViewModel(
        IMessenger messenger,
        ILoggerFactory loggerFactory,        
        NiconicoSession niconicoSession,
        ApplicationLayoutManager applicationLayoutManager,
        NicoVideoProvider nicoVideoProvider,        
        SubscriptionManager subscriptionManager,
        OpenLiveContentCommand openLiveContentCommand,
        VideoPlayWithQueueCommand videoPlayWithQueueCommand
        )
        : base(loggerFactory.CreateLogger<FollowingsActivityPageViewModel>(), disposeItemVM: false)
    {
        _messenger = messenger;
        _niconicoSession = niconicoSession;
        ApplicationLayoutManager = applicationLayoutManager;
        _nicoVideoProvider = nicoVideoProvider;
        SubscriptionManager = subscriptionManager;
        _openLiveContentCommand = openLiveContentCommand;
        VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
    }


    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        if (parameters.TryGetValue("type", out string showType))
        {
            SelectedActivityType = Enum.Parse<ActivityType>(showType);
        }
        else
        {
            SelectedActivityType = ActivityType.Publish;
        }

        _messenger.Register<VideoWatchedMessage>(this);
        _messenger.Register<PlaylistItemAddedMessage>(this);
        _messenger.Register<PlaylistItemRemovedMessage>(this);
        _messenger.Register<ItemIndexUpdatedMessage>(this);
        _messenger.Register<VideoCacheStatusChangedMessage>(this);

        try
        {
            base.OnNavigatedTo(parameters);
        }
        catch
        {
            _messenger.Unregister<VideoWatchedMessage>(this);
            _messenger.Unregister<PlaylistItemAddedMessage>(this);
            _messenger.Unregister<PlaylistItemRemovedMessage>(this);
            _messenger.Unregister<ItemIndexUpdatedMessage>(this);
            _messenger.Unregister<VideoCacheStatusChangedMessage>(this);

            throw;
        }
    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        _messenger.Unregister<VideoWatchedMessage>(this);
        _messenger.Unregister<PlaylistItemAddedMessage>(this);
        _messenger.Unregister<PlaylistItemRemovedMessage>(this);
        _messenger.Unregister<ItemIndexUpdatedMessage>(this);
        _messenger.Unregister<VideoCacheStatusChangedMessage>(this);

        // ニレコポ表示設定をニコレポの設定に書き戻し
        //            ActivityFeedSettings.DisplayFollowingsActivityMuteContextTriggers = DisplayFollowingsActivityMuteContextTriggers.Distinct().ToList();

        base.OnNavigatedFrom(parameters);
    }

    protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode, INavigationParameters parameters)
    {
        /*
        if (!ActivityFeedSettings.DisplayFollowingsActivityMuteContextTriggers.All(x => DisplayFollowingsActivityMuteContextTriggers.Any(y => x == y)))
        {
            DisplayFollowingsActivityMuteContextTriggers.Clear();
            DisplayFollowingsActivityMuteContextTriggers.AddRange(ActivityFeedSettings.DisplayFollowingsActivityMuteContextTriggers);

            _FollowingsActivityMuteContextTriggersChanged = false;
            return true;
        }
        */
        
        return base.CheckNeedUpdateOnNavigateTo(mode, parameters);
    }

    protected override (int, IIncrementalSource<IFollowingsActivityItem>) GenerateIncrementalSource()
    {
        return (LoginUserFollowingsActivityTimelineSource.OneTimeLoadCount, 
            new LoginUserFollowingsActivityTimelineSource(_niconicoSession, _nicoVideoProvider, SubscriptionManager, SelectedActivityType, SelectedActor)
            );
    }


    static IEnumerable<VideoItemViewModel> ToVideoItemVMEnumerable(IEnumerable items)
    {
        foreach (var item in items)
        {
            if (item is VideoItemViewModel videoItemVM)
            {
                yield return videoItemVM;
            }
        }
    }

    void IRecipient<VideoWatchedMessage>.Receive(VideoWatchedMessage message)
    {
        if (ItemsView == null) { return; }
        foreach (var videoItemVM in ToVideoItemVMEnumerable(ItemsView.SourceCollection))
        {
            videoItemVM.OnWatched(message);
        }
    }

    void IRecipient<PlaylistItemAddedMessage>.Receive(PlaylistItemAddedMessage message)
    {
        if (ItemsView == null) { return; }
        foreach (var videoItemVM in ToVideoItemVMEnumerable(ItemsView.SourceCollection))
        {
            videoItemVM.OnPlaylistItemAdded(message);
        }
    }

    void IRecipient<PlaylistItemRemovedMessage>.Receive(PlaylistItemRemovedMessage message)
    {
        if (ItemsView == null) { return; }
        foreach (var videoItemVM in ToVideoItemVMEnumerable(ItemsView.SourceCollection))
        {
            videoItemVM.OnPlaylistItemRemoved(message);
        }
    }

    void IRecipient<ItemIndexUpdatedMessage>.Receive(ItemIndexUpdatedMessage message)
    {
        if (ItemsView == null) { return; }
        foreach (var videoItemVM in ToVideoItemVMEnumerable(ItemsView.SourceCollection))
        {
            videoItemVM.OnQueueItemIndexUpdated(message);
        }
    }

    void IRecipient<VideoCacheStatusChangedMessage>.Receive(VideoCacheStatusChangedMessage message)
    {
        if (ItemsView == null) { return; }
        foreach (var videoItemVM in ToVideoItemVMEnumerable(ItemsView.SourceCollection))
        {
            videoItemVM.OnCacheStatusChanged(message);
        }
    }


    [RelayCommand]
    void OpenFollowingsActivityItem(object item)
    {
        if (item is FollowingsActivityVideoTimeline videoItem)
        {
            var command = VideoPlayWithQueueCommand as ICommand;
            if (command.CanExecute(videoItem))
            {
                command.Execute(videoItem);
            }
        }
        else if (item is FollowingsActivityLiveTimeline liveItem)
        {
            var command = _openLiveContentCommand as ICommand;
            if (command.CanExecute(liveItem))
            {
                command.Execute(liveItem);
            }
        }
    }
}


public class FollowingsActivityLiveTimeline : LiveInfoListItemViewModel, ILiveContent, IFollowingsActivityItem
{
    public NiconicoToolkit.FollowingsActivity.Activity Activity { get; }

    public string OptionText { get; }

    public string Title { get; }

    public FollowingsActivityLiveTimeline(NiconicoToolkit.FollowingsActivity.Activity activity) 
        : base(activity.Content.Id)
    {
        Activity = activity;
        ItemTopic = activity.Kind;
        LiveTitle = activity.Content.Title;
        StartTime = activity.CreatedAt;

        OptionText = "LiveStreamingStartAtWithDateTime".Translate(Activity.Content.StartedAt.ToString("f"));

        Title = Activity.Label.Text;
        ThumbnailUrl = Activity.ThumbnailUrl;
        CommunityThumbnail = Activity.Actor.IconUrl;
        CommunityGlobalId = Activity.Actor.Id;
        CommunityName = Activity.Actor.Name;            

        ItempTopicDescription = Activity.Message.Text;
    }

    public string ItempTopicDescription { get; }

    public string ItemTopic { get; }

    public string BroadcasterId => CommunityGlobalId.ToString();
}

public interface IFollowingsActivityItem
{
    string ItemTopic { get; }
}

public class FollowingsActivityVideoTimeline : VideoListItemControlViewModel, IVideoContent, IFollowingsActivityItem
{
    private readonly NiconicoToolkit.FollowingsActivity.Activity _activity;

    public FollowingsActivityVideoTimeline(NiconicoToolkit.FollowingsActivity.Activity activity) 
        : base(activity.Content.Id, activity.Content.Title, activity.ThumbnailUrl, TimeSpan.Zero, activity.Content.StartedAt)
    {
        _activity = activity;
        ItemTopic = activity.Kind;

        ProviderName = activity.Actor.Name;
        ProviderId = activity.Actor.Id;
        ProviderType = activity.Actor.Type switch 
        {
            NiconicoToolkit.FollowingsActivity.Actor.ActorType_User => OwnerType.User,
            NiconicoToolkit.FollowingsActivity.Actor.ActorType_Channel => OwnerType.Channel,
        };
        ProviderIconUrl = activity.Actor.IconUrl;

        ItempTopicDescription = _activity.Message.Text;
    }        

    public string ItempTopicDescription { get; }

    public string ItemTopic { get; private set; }
}


public static class FollowingsActivityTimelineVM 
{
  
    //public static string ItemTopictypeToDescription(FollowingsActivityMuteContextTrigger topicType, FollowingsActivityEntry timelineItem)
    //{
    //    switch (topicType)
    //    {
    //        case FollowingsActivityMuteContextTrigger.Unknown:
    //            return "Unknown".Translate();
    //        case FollowingsActivityMuteContextTrigger.NicoVideo_User_Video_Upload:
    //            return "FollowingsActivity_Video_UserVideoUpload".Translate(timelineItem.Actor.Name);
    //        case FollowingsActivityMuteContextTrigger.NicoVideo_User_Mylist_Add_Video:
    //            return "FollowingsActivity_Video_UserMylistAddVideo".Translate(timelineItem.Actor.Name);
    //        case FollowingsActivityMuteContextTrigger.NicoVideo_User_Community_Video_Add:
    //            return "FollowingsActivity_Video_CommunityAddVideo".Translate(timelineItem.Actor.Name);
    //        case FollowingsActivityMuteContextTrigger.NicoVideo_Channel_Video_Upload:
    //            return "FollowingsActivity_Video_ChannelVideoUpload".Translate(timelineItem.Actor.Name);
    //        case FollowingsActivityMuteContextTrigger.Live_User_Program_OnAirs:
    //            return "FollowingsActivity_Live_UserProgramOnAirs".Translate(timelineItem.Actor.Name);
    //        case FollowingsActivityMuteContextTrigger.Live_User_Program_Reserve:
    //            return "FollowingsActivity_Live_UserProgramReserve".Translate(timelineItem.Actor.Name);
    //        case FollowingsActivityMuteContextTrigger.Live_Channel_Program_Onairs:
    //            return "FollowingsActivity_Live_ChannelProgramOnAirs".Translate(timelineItem.Actor.Name);
    //        case FollowingsActivityMuteContextTrigger.Live_Channel_Program_Reserve:
    //            return "FollowingsActivity_Live_ChannelProgramReserve".Translate(timelineItem.Actor.Name);
    //        default:
    //            return string.Empty;
    //    }
    //}


    /*
    private string _OwnerUserId;
    public string ProviderId => _OwnerUserId;

    public string ProviderName { get; private set; }

    public UserType ProviderType { get; private set; }

    public string Id => TimelineItem.Video?.Id ?? TimelineItem.Program.Id;        
    */
}


public class LoginUserFollowingsActivityTimelineSource : IIncrementalSource<IFollowingsActivityItem>
{
    // 通常10だが、ニコレポの表示フィルタを掛けた場合に
    // 追加読み込み時に表示対象が見つからない場合
    // 追加読み込みが途絶えるため、多めに設定している
    public const int OneTimeLoadCount = 25;

    public SubscriptionManager SubscriptionManager { get; }
    public ActivityType ActivityType { get; }
    public NiconicoActivityActor? ActivityActor { get; }

    public LoginUserFollowingsActivityTimelineSource(
        NiconicoSession niconicoSession,
        NicoVideoProvider nicoVideoProvider,
        SubscriptionManager subscriptionManager,
        ActivityType activityType,
        NiconicoActivityActor? activityActor
        )
    {
        _niconicoSession = niconicoSession;
        _nicoVideoProvider = nicoVideoProvider;
        SubscriptionManager = subscriptionManager;
        ActivityType = activityType;
        ActivityActor = activityActor;
    }

    DateTime NiconamaDisplayTime = DateTime.Now - TimeSpan.FromHours(6);
    private readonly NiconicoContext _niconicoContext;
    private readonly NiconicoSession _niconicoSession;
    private readonly NicoVideoProvider _nicoVideoProvider;

    FollowingsActivityResponse _prevRes;

    //public static bool IsLiveTopic(FollowingsActivityMuteContextTrigger topic)
    //{
    //    return topic
    //        is FollowingsActivityMuteContextTrigger.Live_User_Program_OnAirs
    //        or FollowingsActivityMuteContextTrigger.Live_User_Program_Reserve
    //        or FollowingsActivityMuteContextTrigger.Live_Channel_Program_Onairs
    //        or FollowingsActivityMuteContextTrigger.Live_Channel_Program_Reserve
    //        ;
    //}

    //public static bool IsVideoTopic(FollowingsActivityMuteContextTrigger topic)
    //{
    //    return topic
    //        is FollowingsActivityMuteContextTrigger.NicoVideo_User_Video_Upload
    //        or FollowingsActivityMuteContextTrigger.NicoVideo_User_Mylist_Add_Video
    //        or FollowingsActivityMuteContextTrigger.NicoVideo_Channel_Video_Upload
    //        ;
    //}

    async Task<IEnumerable<IFollowingsActivityItem>> IIncrementalSource<IFollowingsActivityItem>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
    {
        if (_prevRes != null && _prevRes.NextCursor == null)
        {
            return [];
        }

        var res = await _niconicoSession.ToolkitContext.FollowingsActivity.GetFollowingsActivityAsync(ActivityType, ActivityActor, _prevRes?.NextCursor, ct);

        ct.ThrowIfCancellationRequested();

        if (res.IsOk is false) { return Enumerable.Empty<IFollowingsActivityItem>(); }

        _prevRes = res;
#if DEBUG
        List<string> triggers = new List<string>();
#endif

        Debug.WriteLine(string.Join(" / ", res.Activities.Select(X => X.Kind).Distinct()));

        return res.Activities.Select(item =>
        {            
            try
            {
                if (item.Content.Type == Content.ContentType_LiveProgram)
                {
                    return new FollowingsActivityLiveTimeline(item) as IFollowingsActivityItem;
                }
                else if (item.Content.Type == Content.ContentType_Video)
                {
                    return new FollowingsActivityVideoTimeline(item) as IFollowingsActivityItem;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        })
            .Where(x => x != null)
            .ToArray()// Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
            ;

#if DEBUG
        //Debug.WriteLine(string.Join(" ", triggers));
#endif
    }
}



//public class SelectableFollowingsActivityMuteContextTrigger
//{
//    public FollowingsActivityMuteContextTrigger Topic { get; set; }
//}
