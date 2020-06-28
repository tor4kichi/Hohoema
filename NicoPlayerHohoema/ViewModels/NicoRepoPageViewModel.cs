using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Mntone.Nico2.NicoRepo;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Helpers;
using Prism.Commands;
using Reactive.Bindings;
using System.Collections.ObjectModel;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Mntone.Nico2.Live;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Services;
using Prism.Navigation;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.UseCase.Playlist;
using NicoPlayerHohoema.UseCase.NicoVideoPlayer.Commands;
using NicoPlayerHohoema.UseCase;
using I18NPortable;
using System.Reactive.Concurrency;
using System.Threading;
using System.Runtime.CompilerServices;

namespace NicoPlayerHohoema.ViewModels
{
    public class NicoRepoPageViewModel : HohoemaListingPageViewModelBase<HohoemaListingPageItemBase>, INavigatedAwareAsync
    {
        public NicoRepoPageViewModel(
            IScheduler scheduler,
            ApplicationLayoutManager applicationLayoutManager,
            HohoemaPlaylist hohoemaPlaylist,
            Services.PageManager pageManager,
            ActivityFeedSettings activityFeedSettings,
            Models.Provider.LoginUserNicoRepoProvider loginUserNicoRepoProvider,
            Models.Subscription.SubscriptionManager subscriptionManager,
            OpenLiveContentCommand openLiveContentCommand
            )
        {
            _scheduler = scheduler;
            ApplicationLayoutManager = applicationLayoutManager;
            HohoemaPlaylist = hohoemaPlaylist;
            ActivityFeedSettings = activityFeedSettings;
            LoginUserNicoRepoProvider = loginUserNicoRepoProvider;
            SubscriptionManager = subscriptionManager;
            _openLiveContentCommand = openLiveContentCommand;
            DisplayNicoRepoItemTopics = new ObservableCollection<NicoRepoItemTopic>(ActivityFeedSettings.DisplayNicoRepoItemTopics);

            DisplayNicoRepoItemTopics.CollectionChangedAsObservable()
                .Subscribe(_ =>
                {
                    _NicoRepoItemTopicsChanged = true;
                })
                .AddTo(_CompositeDisposable);
        }

        bool _NicoRepoItemTopicsChanged;

        public static IList<NicoRepoItemTopic> DisplayCandidateNicoRepoItemTopicList { get; } = new List<NicoRepoItemTopic>()
        {
            NicoRepoItemTopic.NicoVideo_User_Video_Upload,
            NicoRepoItemTopic.NicoVideo_User_Mylist_Add_Video,
            NicoRepoItemTopic.NicoVideo_Channel_Video_Upload,
            NicoRepoItemTopic.Live_Channel_Program_Onairs,
            NicoRepoItemTopic.Live_Channel_Program_Reserve,
            NicoRepoItemTopic.Live_User_Program_OnAirs,
            NicoRepoItemTopic.Live_User_Program_Reserve,
        };

        public ObservableCollection<NicoRepoItemTopic> DisplayNicoRepoItemTopics { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public ActivityFeedSettings ActivityFeedSettings { get; }
        public Models.Provider.LoginUserNicoRepoProvider LoginUserNicoRepoProvider { get; }
        public Models.Subscription.SubscriptionManager SubscriptionManager { get; }



        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            // ニレコポ表示設定をニコレポの設定に書き戻し
            var saveTopics = DisplayNicoRepoItemTopics.Distinct().ToList();
            if (saveTopics.Count == 0)
            {
                saveTopics.Add(NicoRepoItemTopic.NicoVideo_User_Video_Upload);
            }
            ActivityFeedSettings.DisplayNicoRepoItemTopics = DisplayNicoRepoItemTopics.Distinct().ToList();
            ActivityFeedSettings.Save().ConfigureAwait(false);

            base.OnNavigatedFrom(parameters);
        }

        protected override IIncrementalSource<HohoemaListingPageItemBase> GenerateIncrementalSource()
        {
            if (DisplayNicoRepoItemTopics.Count == 0)
            {
                DisplayNicoRepoItemTopics.Add(NicoRepoItemTopic.NicoVideo_User_Video_Upload);
            }
            return new LoginUserNicoRepoTimelineSource(LoginUserNicoRepoProvider, SubscriptionManager, DisplayNicoRepoItemTopics);
        }


        DelegateCommand<object> _openNicoRepoItemCommand;
        private readonly IScheduler _scheduler;
        private readonly OpenLiveContentCommand _openLiveContentCommand;

        public DelegateCommand<object> OpenNicoRepoItemCommand => _openNicoRepoItemCommand
            ?? (_openNicoRepoItemCommand = new DelegateCommand<object>(item => 
            {
                if (item is NicoRepoVideoTimeline videoItem)
                {
                    HohoemaPlaylist.Play(videoItem);
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


        public void OnResetNicoRepoItemTopicsEditCompleted()
        {
            if (_NicoRepoItemTopicsChanged)
            {
                _ = ResetList();

                _NicoRepoItemTopicsChanged = false;
            }
        }

    }


    public class NicoRepoLiveTimeline : LiveInfoListItemViewModel, Interfaces.ILiveContent
    {
        public NicoRepoLiveTimeline(NicoRepoTimelineItem timelineItem, NicoRepoItemTopic itemTopic) 
            : base(timelineItem.Program.Id)
        {
            TimelineItem = timelineItem;
            ItemTopic = itemTopic;
            if (TimelineItem.Program != null)
            {
                this.Label = TimelineItem.Program.Title;
                AddImageUrl(TimelineItem.Program.ThumbnailUrl);
                this.OptionText = "LiveStreamingStartAtWithDateTime".Translate(TimelineItem.Program.BeginAt.ToString());
                CommunityThumbnail = TimelineItem.Program.ThumbnailUrl;
                if (TimelineItem.Community != null)
                {
                    CommunityGlobalId = TimelineItem.Community.Id;
                    CommunityName = TimelineItem.Community.Name;
                }
                else
                {
                    CommunityGlobalId = TimelineItem.SenderChannel.Id.ToString();
                    CommunityName = TimelineItem.SenderChannel.Name;
                }
            }

            if (timelineItem.SenderChannel != null)
            {
                this.CommunityType = CommunityType.Channel;
            }
            else if (timelineItem.Community != null)
            {
                this.CommunityType = CommunityType.Community;
            }
            else
            {
                this.CommunityType = CommunityType.Official;
            }

            ItempTopicDescription = NicoRepoTimelineVM.ItemTopictypeToDescription(ItemTopic, TimelineItem);
        }

        public string ItempTopicDescription { get; }


        public NicoRepoTimelineItem TimelineItem { get; private set; }

        public NicoRepoItemTopic ItemTopic { get; private set; }

        public string BroadcasterId => CommunityGlobalId.ToString();
    }

    public class NicoRepoVideoTimeline : VideoInfoControlViewModel, IVideoContent
    {
        public NicoRepoVideoTimeline(NicoRepoTimelineItem timelineItem, NicoRepoItemTopic itemType) 
            : base(timelineItem.Video.Id)
        {
            TimelineItem = timelineItem;
            ItemTopic = itemType;

            if (TimelineItem.Video != null)
            {
                this.Label = TimelineItem.Video.Title;
                if (TimelineItem.Video.ThumbnailUrl.Small != null)
                {
                    AddImageUrl(TimelineItem.Video.ThumbnailUrl.Small);
                }
                else if (TimelineItem.Video.ThumbnailUrl.Normal != null)
                {
                    AddImageUrl(TimelineItem.Video.ThumbnailUrl.Normal);
                }
                this.OptionText = $"{TimelineItem.CreatedAt.ToString()}";
            }

            ItempTopicDescription = NicoRepoTimelineVM.ItemTopictypeToDescription(ItemTopic, TimelineItem);
            /*

            if (TimelineItem.SenderNiconicoUser != null)
            {
                ProviderId = TimelineItem.SenderNiconicoUser.Id.ToString();
                ProviderName = TimelineItem.SenderNiconicoUser.Nickname;
                ProviderType = UserType.User;
            }


            if (TimelineItem.SenderNiconicoUser != null)
            {
                this.Description = this.TimelineItem.SenderNiconicoUser.Nickname;
            }
            else if (TimelineItem.SenderChannel != null)
            {
                this.Description = this.TimelineItem.SenderChannel.Name;
                _OwnerUserId = TimelineItem.SenderChannel.Id.ToString();
                ProviderType = UserType.Channel;
            }
            */

        }


        public string ItempTopicDescription { get; }

        public NicoRepoTimelineItem TimelineItem { get; private set; }

        public NicoRepoItemTopic ItemTopic { get; private set; }
    }


    public static class NicoRepoTimelineVM 
    {
      
        public static string ItemTopictypeToDescription(NicoRepoItemTopic topicType, NicoRepoTimelineItem timelineItem)
        {
            switch (topicType)
            {
                case NicoRepoItemTopic.Unknown:
                    return "Unknown".Translate();
                case NicoRepoItemTopic.NicoVideo_User_Video_Upload:
                    return "NicoRepo_Video_UserVideoUpload".Translate(timelineItem.SenderNiconicoUser.Nickname);
                case NicoRepoItemTopic.NicoVideo_User_Mylist_Add_Video:
                    return "NicoRepo_Video_UserMylistAddVideo".Translate(timelineItem.SenderNiconicoUser.Nickname);
                case NicoRepoItemTopic.NicoVideo_User_Community_Video_Add:
                    return "NicoRepo_Video_CommunityAddVideo".Translate(timelineItem.Community.Name);
                case NicoRepoItemTopic.NicoVideo_Channel_Video_Upload:
                    return "NicoRepo_Video_ChannelVideoUpload".Translate(timelineItem.SenderChannel.Name);
                case NicoRepoItemTopic.Live_User_Program_OnAirs:
                    return "NicoRepo_Live_UserProgramOnAirs".Translate(timelineItem.SenderNiconicoUser.Nickname);
                case NicoRepoItemTopic.Live_User_Program_Reserve:
                    return "NicoRepo_Live_UserProgramReserve".Translate(timelineItem.SenderNiconicoUser.Nickname);
                case NicoRepoItemTopic.Live_Channel_Program_Onairs:
                    return "NicoRepo_Live_ChannelProgramOnAirs".Translate(timelineItem.SenderChannel.Name);
                case NicoRepoItemTopic.Live_Channel_Program_Reserve:
                    return "NicoRepo_Live_ChannelProgramReserve".Translate(timelineItem.SenderChannel.Name);
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


    public class LoginUserNicoRepoTimelineSource : HohoemaIncrementalSourceBase<HohoemaListingPageItemBase>
    {
        public List<NicoRepoItemTopic> AllowedNicoRepoItemType { get; }



        List<NicoRepoTimelineItem> TimelineItems { get; } = new List<NicoRepoTimelineItem>();

        NicoRepoTimelineItem _LastItem = null;

        // 通常10だが、ニコレポの表示フィルタを掛けた場合に
        // 追加読み込み時に表示対象が見つからない場合
        // 追加読み込みが途絶えるため、多めに設定している
        public override uint OneTimeLoadCount => 10;

        public Models.Provider.LoginUserNicoRepoProvider LoginUserNicoRepoProvider { get; }
        public Models.Subscription.SubscriptionManager SubscriptionManager { get; }

        public LoginUserNicoRepoTimelineSource(
            Models.Provider.LoginUserNicoRepoProvider loginUserNicoRepoProvider,
            Models.Subscription.SubscriptionManager subscriptionManager,
            IEnumerable<NicoRepoItemTopic> allowedNicoRepoTypes
            )
        {
            AllowedNicoRepoItemType = allowedNicoRepoTypes.ToList();
            LoginUserNicoRepoProvider = loginUserNicoRepoProvider;
            SubscriptionManager = subscriptionManager;
        }



        protected override async Task<int> ResetSourceImpl()
        {
            TimelineItems.Clear();

            var nicoRepoResponse = await LoginUserNicoRepoProvider.GetLoginUserNicoRepo(NicoRepoTimelineType.all);

            if (nicoRepoResponse.IsStatusOK)
            {
                foreach (var item in nicoRepoResponse.TimelineItems)
                {
                    if (CheckCanDisplayTimelineItem(item))
                    {
                        TimelineItems.Add(item);
                    }
                }
                _LastItem = nicoRepoResponse.LastTimelineItem;
                return nicoRepoResponse.Meta.Limit;
            }
            else
            {
                return 0;
            }
        }


        private bool CheckCanDisplayTimelineItem(NicoRepoTimelineItem item)
        {
            var topicType = NicoRepoItemTopicExtension.ToNicoRepoTopicType(item.Topic);
            if (!AllowedNicoRepoItemType.Any(x => x == topicType))
            {
                return false;
            }

            if (topicType == NicoRepoItemTopic.Live_User_Program_OnAirs
            || topicType == NicoRepoItemTopic.Live_User_Program_Reserve
            || topicType == NicoRepoItemTopic.Live_Channel_Program_Onairs
            || topicType == NicoRepoItemTopic.Live_Channel_Program_Reserve)
            {
                if (item.Program != null)
                {
                    // 放送開始が現時点より６時間以上前の場合には既に終了済みとして表示しない
                    if (item.Program.BeginAt < NiconamaDisplayTime)
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return true;
            }
        }


        DateTime NiconamaDisplayTime = DateTime.Now - TimeSpan.FromHours(6);

        protected override async IAsyncEnumerable<HohoemaListingPageItemBase> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var tail = head + count;
            var prevCount = TimelineItems.Count;

            if (TimelineItems.Count < tail)
            {
                while (prevCount == TimelineItems.Count)
                {
                    var nicoRepoResponse = await LoginUserNicoRepoProvider.GetLoginUserNicoRepo(NicoRepoTimelineType.all, _LastItem?.Id);
                    if (nicoRepoResponse.IsStatusOK)
                    {
                        foreach (var item in nicoRepoResponse.TimelineItems)
                        {
                            if (CheckCanDisplayTimelineItem(item))
                            {
                                TimelineItems.Add(item);
                            }
                        }
                        _LastItem = nicoRepoResponse.LastTimelineItem;

                        if (nicoRepoResponse.TimelineItems.Count == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            foreach (var item in TimelineItems.Skip(head).Take(count))
            {
                var topicType = NicoRepoItemTopicExtension.ToNicoRepoTopicType(item.Topic);
                if (topicType == NicoRepoItemTopic.Live_User_Program_OnAirs
                || topicType == NicoRepoItemTopic.Live_User_Program_Reserve
                || topicType == NicoRepoItemTopic.Live_Channel_Program_Onairs
                || topicType == NicoRepoItemTopic.Live_Channel_Program_Reserve)
                {
                    yield return new NicoRepoLiveTimeline(item, topicType);
                }
                else if (topicType == NicoRepoItemTopic.NicoVideo_User_Video_Upload ||
                        topicType == NicoRepoItemTopic.NicoVideo_User_Mylist_Add_Video ||
                        topicType == NicoRepoItemTopic.NicoVideo_Channel_Video_Upload)
                {
                    var videoItem = new NicoRepoVideoTimeline(item, topicType);
                    await videoItem.InitializeAsync(cancellationToken);
                    yield return videoItem;
                }
                else
                {
                    throw new NotSupportedException(topicType.ToString());
                }
            }
        }
    }



    public enum NicoRepoItemTopic
    {
        Unknown,
        NicoVideo_User_Video_Kiriban_Play,
        NicoVideo_User_Video_Upload,
        NicoVideo_Community_Level_Raise,
        NicoVideo_User_Mylist_Add_Video,
        NicoVideo_User_Community_Video_Add,
        NicoVideo_User_Video_UpdateHighestRankings,
        NicoVideo_User_Video_Advertise,
        NicoVideo_Channel_Blomaga_Upload,
        NicoVideo_Channel_Video_Upload,
        Live_User_Program_OnAirs,
        Live_User_Program_Reserve,
        Live_Channel_Program_Onairs,
        Live_Channel_Program_Reserve,
    }

    public static class NicoRepoItemTopicExtension
    {
        public static NicoRepoItemTopic ToNicoRepoTopicType(string topic)
        {
            NicoRepoItemTopic topicType = NicoRepoItemTopic.Unknown;
            switch (topic)
            {
                case "live.user.program.onairs":
                    topicType = NicoRepoItemTopic.Live_User_Program_OnAirs;
                    break;
                case "live.user.program.reserve":
                    topicType = NicoRepoItemTopic.Live_User_Program_Reserve;
                    break;
                case "nicovideo.user.video.kiriban.play":
                    topicType = NicoRepoItemTopic.NicoVideo_User_Video_Kiriban_Play;
                    break;
                case "nicovideo.user.video.upload":
                    topicType = NicoRepoItemTopic.NicoVideo_User_Video_Upload;
                    break;
                case "nicovideo.community.level.raise":
                    topicType = NicoRepoItemTopic.NicoVideo_Community_Level_Raise;
                    break;
                case "nicovideo.user.mylist.add.video":
                    topicType = NicoRepoItemTopic.NicoVideo_User_Mylist_Add_Video;
                    break;
                case "nicovideo.user.community.video.add":
                    topicType = NicoRepoItemTopic.NicoVideo_User_Community_Video_Add;
                    break;
                case "nicovideo.user.video.update_highest_rankings":
                    topicType = NicoRepoItemTopic.NicoVideo_User_Video_UpdateHighestRankings;
                    break;
                case "nicovideo.user.video.advertise":
                    topicType = NicoRepoItemTopic.NicoVideo_User_Video_Advertise;
                    break;
                case "nicovideo.channel.blomaga.upload":
                    topicType = NicoRepoItemTopic.NicoVideo_Channel_Blomaga_Upload;
                    break;
                case "nicovideo.channel.video.upload":
                    topicType = NicoRepoItemTopic.NicoVideo_Channel_Video_Upload;
                    break;
                case "live.channel.program.onairs":
                    topicType = NicoRepoItemTopic.Live_Channel_Program_Onairs;
                    break;
                case "live.channel.program.reserve":
                    topicType = NicoRepoItemTopic.Live_Channel_Program_Reserve;
                    break;
                default:
                    break;
            }

            return topicType;
        }
    }

    public class SelectableNicoRepoItemTopic
    {
        public NicoRepoItemTopic Topic { get; set; }
    }
}
