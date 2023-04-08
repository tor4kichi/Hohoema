using CommunityToolkit.Mvvm.Input;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.NicoRepo;
using Hohoema.Models.Niconico.NicoRepo.LoginUser;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Subscriptions;
using Hohoema.Services;
using Hohoema.ViewModels.Niconico.Live;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.VideoListPage;
using I18NPortable;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Live;
using NiconicoToolkit.NicoRepo;
using NiconicoToolkit.Video;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Navigation;

namespace Hohoema.ViewModels.Pages.Niconico.NicoRepo;

public class NicoRepoPageViewModel : HohoemaListingPageViewModelBase<INicoRepoItem>
{
    public NicoRepoPageViewModel(
        ILoggerFactory loggerFactory,
        IScheduler scheduler,
        ApplicationLayoutManager applicationLayoutManager,
        NicoVideoProvider nicoVideoProvider,
        PageManager pageManager,
        NicoRepoSettings activityFeedSettings,
        LoginUserNicoRepoProvider loginUserNicoRepoProvider,
        SubscriptionManager subscriptionManager,
        OpenLiveContentCommand openLiveContentCommand,
        VideoPlayWithQueueCommand videoPlayWithQueueCommand
        )
        : base(loggerFactory.CreateLogger<NicoRepoPageViewModel>())
    {
        _scheduler = scheduler;
        ApplicationLayoutManager = applicationLayoutManager;
        _nicoVideoProvider = nicoVideoProvider;
        ActivityFeedSettings = activityFeedSettings;
        LoginUserNicoRepoProvider = loginUserNicoRepoProvider;
        SubscriptionManager = subscriptionManager;
        _openLiveContentCommand = openLiveContentCommand;
        VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
        NicoRepoType = new ReactiveProperty<NicoRepoType>(NiconicoToolkit.NicoRepo.NicoRepoType.Video, mode:ReactivePropertyMode.DistinctUntilChanged);
        NicoRepoDisplayTarget = new ReactiveProperty<NicoRepoDisplayTarget>(mode: ReactivePropertyMode.DistinctUntilChanged);

        new[]
        {
            NicoRepoType.ToUnit(),
            NicoRepoDisplayTarget.ToUnit()
        }
        .Merge()
        .Subscribe(_ => 
        {
            ResetList();
        })
        .AddTo(_CompositeDisposable);
    }

    bool _NicoRepoMuteContextTriggersChanged;

    public ImmutableArray<NicoRepoType> NicoRepoTypeList { get; } = new[]
    {
        NiconicoToolkit.NicoRepo.NicoRepoType.All,
        NiconicoToolkit.NicoRepo.NicoRepoType.Video,
        NiconicoToolkit.NicoRepo.NicoRepoType.Program,
    }
    .ToImmutableArray();


    public ReactiveProperty<NicoRepoType> NicoRepoType { get; }
    public ReactiveProperty<NicoRepoDisplayTarget> NicoRepoDisplayTarget { get; }

    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public NicoRepoSettings ActivityFeedSettings { get; }
    public LoginUserNicoRepoProvider LoginUserNicoRepoProvider { get; }
    public SubscriptionManager SubscriptionManager { get; }
    public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }

    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        if (parameters.TryGetValue("type", out string showType))
        {
            NicoRepoType.Value = Enum.Parse<NicoRepoType>(showType);
        }

        base.OnNavigatedTo(parameters);
    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        // ニレコポ表示設定をニコレポの設定に書き戻し
//            ActivityFeedSettings.DisplayNicoRepoMuteContextTriggers = DisplayNicoRepoMuteContextTriggers.Distinct().ToList();

        base.OnNavigatedFrom(parameters);
    }

    protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode, INavigationParameters parameters)
    {
        /*
        if (!ActivityFeedSettings.DisplayNicoRepoMuteContextTriggers.All(x => DisplayNicoRepoMuteContextTriggers.Any(y => x == y)))
        {
            DisplayNicoRepoMuteContextTriggers.Clear();
            DisplayNicoRepoMuteContextTriggers.AddRange(ActivityFeedSettings.DisplayNicoRepoMuteContextTriggers);

            _NicoRepoMuteContextTriggersChanged = false;
            return true;
        }
        */
        
        return base.CheckNeedUpdateOnNavigateTo(mode, parameters);
    }

    protected override (int, IIncrementalSource<INicoRepoItem>) GenerateIncrementalSource()
    {
        return (LoginUserNicoRepoTimelineSource.OneTimeLoadCount, new LoginUserNicoRepoTimelineSource(LoginUserNicoRepoProvider, _nicoVideoProvider, SubscriptionManager, NicoRepoType.Value, NicoRepoDisplayTarget.Value));
    }


    RelayCommand<object> _openNicoRepoItemCommand;
    private readonly IScheduler _scheduler;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly OpenLiveContentCommand _openLiveContentCommand;

    public RelayCommand<object> OpenNicoRepoItemCommand => _openNicoRepoItemCommand
        ?? (_openNicoRepoItemCommand = new RelayCommand<object>(item => 
        {
            if (item is NicoRepoVideoTimeline videoItem)
            {
                var command = VideoPlayWithQueueCommand as ICommand;
                if (command.CanExecute(videoItem))
                {
                    command.Execute(videoItem);
                }
            }
            else if (item is NicoRepoLiveTimeline liveItem)
            {
                var command = _openLiveContentCommand as ICommand;
                if (command.CanExecute(liveItem))
                {
                    command.Execute(liveItem);
                }
            }
        }));
}


public class NicoRepoLiveTimeline : LiveInfoListItemViewModel, ILiveContent, INicoRepoItem
{
    private readonly NicoRepoEntry _nicoRepoEntry;

    public string OptionText { get; }

    public string Title { get; }

    public NicoRepoLiveTimeline(NicoRepoEntry nicoRepoEntry, NicoRepoMuteContextTrigger itemTopic) 
        : base(nicoRepoEntry.GetContentId())
    {
        _nicoRepoEntry = nicoRepoEntry;
        ItemTopic = itemTopic;
        LiveTitle = nicoRepoEntry.Object.Name;
        StartTime = _nicoRepoEntry.Updated.LocalDateTime;

        OptionText = "LiveStreamingStartAtWithDateTime".Translate(_nicoRepoEntry.Updated.ToString("f"));

        Title = _nicoRepoEntry.Title;
        ThumbnailUrl = _nicoRepoEntry.Object.Image.OriginalString;
        CommunityThumbnail = _nicoRepoEntry.Actor.Icon.OriginalString;
        CommunityGlobalId = _nicoRepoEntry.MuteContext.Sender.Id;
        CommunityName = _nicoRepoEntry.Actor.Name;            

        this.CommunityType = _nicoRepoEntry.MuteContext.Sender.IdType switch
        {
            SenderIdTypeEnum.User => ProviderType.Community,
            SenderIdTypeEnum.Channel => ProviderType.Channel,
            _ => throw new NotSupportedException()
        };

        ItempTopicDescription = NicoRepoTimelineVM.ItemTopictypeToDescription(ItemTopic, _nicoRepoEntry);
    }

    public string ItempTopicDescription { get; }

    public NicoRepoMuteContextTrigger ItemTopic { get; private set; }

    public string BroadcasterId => CommunityGlobalId.ToString();
}

public interface INicoRepoItem
{
    NicoRepoMuteContextTrigger ItemTopic { get; }
}

public class NicoRepoVideoTimeline : VideoListItemControlViewModel, IVideoContent, INicoRepoItem
{
    private readonly NicoRepoEntry _nicoRepoEntry;

    public NicoRepoVideoTimeline(NicoRepoEntry nicoRepoEntry, NicoRepoMuteContextTrigger itemType) 
        : base(nicoRepoEntry.GetContentId(), nicoRepoEntry.Object.Name, nicoRepoEntry.Object.Image.OriginalString, TimeSpan.Zero, nicoRepoEntry.Updated.DateTime)
    {
        _nicoRepoEntry = nicoRepoEntry;
        ItemTopic = itemType;

        //VideoId = nicoRepoEntry.GetContentId();
        if (VideoId != VideoId)
        {
            SubscribeAll(VideoId);
        }

        if (_nicoRepoEntry.Actor != null)
        {
            if (_nicoRepoEntry.Actor.Url.OriginalString.StartsWith("https://ch.nicovideo.jp/"))
            {
                
                // チャンネル
                ProviderName = _nicoRepoEntry.Actor.Name;

                try
                {
                    var iconFileName = _nicoRepoEntry.Actor.Icon.Segments.Last();
                    ProviderId = new String(iconFileName.TakeWhile(c => c != '.').ToArray());
                }
                catch
                {
                    ProviderId = _nicoRepoEntry.Actor.Url.Segments.Last();
                }

                ProviderType = OwnerType.Channel;
            }
            else
            {
                ProviderName = _nicoRepoEntry.Actor.Name;
                ProviderId = _nicoRepoEntry.Actor.Url.Segments.Last();
                ProviderType = OwnerType.User;
            }
        }
        
        //SetLength(nicoVideo.Length);
        ItempTopicDescription = NicoRepoTimelineVM.ItemTopictypeToDescription(ItemTopic, _nicoRepoEntry);
    }        

    public string ItempTopicDescription { get; }

    public NicoRepoMuteContextTrigger ItemTopic { get; private set; }

    protected override void OnInitialized()
    {
    }
}


public static class NicoRepoTimelineVM 
{
  
    public static string ItemTopictypeToDescription(NicoRepoMuteContextTrigger topicType, NicoRepoEntry timelineItem)
    {
        switch (topicType)
        {
            case NicoRepoMuteContextTrigger.Unknown:
                return "Unknown".Translate();
            case NicoRepoMuteContextTrigger.NicoVideo_User_Video_Upload:
                return "NicoRepo_Video_UserVideoUpload".Translate(timelineItem.Actor.Name);
            case NicoRepoMuteContextTrigger.NicoVideo_User_Mylist_Add_Video:
                return "NicoRepo_Video_UserMylistAddVideo".Translate(timelineItem.Actor.Name);
            case NicoRepoMuteContextTrigger.NicoVideo_User_Community_Video_Add:
                return "NicoRepo_Video_CommunityAddVideo".Translate(timelineItem.Actor.Name);
            case NicoRepoMuteContextTrigger.NicoVideo_Channel_Video_Upload:
                return "NicoRepo_Video_ChannelVideoUpload".Translate(timelineItem.Actor.Name);
            case NicoRepoMuteContextTrigger.Live_User_Program_OnAirs:
                return "NicoRepo_Live_UserProgramOnAirs".Translate(timelineItem.Actor.Name);
            case NicoRepoMuteContextTrigger.Live_User_Program_Reserve:
                return "NicoRepo_Live_UserProgramReserve".Translate(timelineItem.Actor.Name);
            case NicoRepoMuteContextTrigger.Live_Channel_Program_Onairs:
                return "NicoRepo_Live_ChannelProgramOnAirs".Translate(timelineItem.Actor.Name);
            case NicoRepoMuteContextTrigger.Live_Channel_Program_Reserve:
                return "NicoRepo_Live_ChannelProgramReserve".Translate(timelineItem.Actor.Name);
            default:
                return string.Empty;
        }
    }


    /*
    private string _OwnerUserId;
    public string ProviderId => _OwnerUserId;

    public string ProviderName { get; private set; }

    public UserType ProviderType { get; private set; }

    public string Id => TimelineItem.Video?.Id ?? TimelineItem.Program.Id;        
    */
}


public class LoginUserNicoRepoTimelineSource : IIncrementalSource<INicoRepoItem>
{
    // 通常10だが、ニコレポの表示フィルタを掛けた場合に
    // 追加読み込み時に表示対象が見つからない場合
    // 追加読み込みが途絶えるため、多めに設定している
    public const int OneTimeLoadCount = 25;

    public LoginUserNicoRepoProvider LoginUserNicoRepoProvider { get; }
    public SubscriptionManager SubscriptionManager { get; }

    public LoginUserNicoRepoTimelineSource(
        LoginUserNicoRepoProvider loginUserNicoRepoProvider,
        NicoVideoProvider nicoVideoProvider,
        SubscriptionManager subscriptionManager,
        NicoRepoType nicoRepoType,
        NicoRepoDisplayTarget nicoRepoTarget
        )
    {
        LoginUserNicoRepoProvider = loginUserNicoRepoProvider;
        _nicoVideoProvider = nicoVideoProvider;
        SubscriptionManager = subscriptionManager;
        _nicoRepoType = nicoRepoType;
        _nicoRepoDisplayTarget = nicoRepoTarget;
    }

    DateTime NiconamaDisplayTime = DateTime.Now - TimeSpan.FromHours(6);
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly NicoRepoType _nicoRepoType;
    private readonly NicoRepoDisplayTarget _nicoRepoDisplayTarget;

    NicoRepoEntriesResponse _prevRes;

    public static bool IsLiveTopic(NicoRepoMuteContextTrigger topic)
    {
        return topic
            is NicoRepoMuteContextTrigger.Live_User_Program_OnAirs
            or NicoRepoMuteContextTrigger.Live_User_Program_Reserve
            or NicoRepoMuteContextTrigger.Live_Channel_Program_Onairs
            or NicoRepoMuteContextTrigger.Live_Channel_Program_Reserve
            ;
    }

    public static bool IsVideoTopic(NicoRepoMuteContextTrigger topic)
    {
        return topic
            is NicoRepoMuteContextTrigger.NicoVideo_User_Video_Upload
            or NicoRepoMuteContextTrigger.NicoVideo_User_Mylist_Add_Video
            or NicoRepoMuteContextTrigger.NicoVideo_Channel_Video_Upload
            ;
    }

    async Task<IEnumerable<INicoRepoItem>> IIncrementalSource<INicoRepoItem>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
    {
        var nicoRepoResponse = await LoginUserNicoRepoProvider.GetLoginUserNicoRepoAsync(_nicoRepoType, _nicoRepoDisplayTarget, _prevRes);

        ct.ThrowIfCancellationRequested();

        if (nicoRepoResponse.Meta.Status != 200) { return Enumerable.Empty<INicoRepoItem>(); }

        _prevRes = nicoRepoResponse;
#if DEBUG
        List<string> triggers = new List<string>();
#endif

        // Note: ニコレポで取得できるチャンネル動画は基本的に動画IDが数字のみで、"so1234567" みたいな形式ではない
        // このためアプリ内での扱いを
        var topicTypeMapedEntries = nicoRepoResponse.Data.Select(x => (TopicType: x.GetMuteContextTrigger(), Item: x)).ToList();
        var numberIdVideoTopics = topicTypeMapedEntries
            .Where(x => IsVideoTopic(x.TopicType));

        return topicTypeMapedEntries.Select(item =>
        {
#if DEBUG
            if (triggers.Any(x => x == item.Item.MuteContext.Trigger) is false)
            {
                triggers.Add(item.Item.MuteContext.Trigger);
            }
#endif
            var topicType = item.TopicType;
            if (IsLiveTopic(topicType))
            {
                return new NicoRepoLiveTimeline(item.Item, topicType) as INicoRepoItem;
            }
            else if (IsVideoTopic(topicType))
            {
                try
                {
                    var id = item.Item.GetContentId();
                    var vm = new NicoRepoVideoTimeline(item.Item, topicType);
                    return vm as INicoRepoItem;
                }
                catch
                {
                    return null;
                }
            }
            else
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



public class SelectableNicoRepoMuteContextTrigger
{
    public NicoRepoMuteContextTrigger Topic { get; set; }
}
