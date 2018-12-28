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
using Prism.Windows.Navigation;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Collections.Async;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Live;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Services;

namespace NicoPlayerHohoema.ViewModels
{
    public class NicoRepoPageViewModel : HohoemaListingPageViewModelBase<HohoemaListingPageItemBase>
    {
        public NicoRepoPageViewModel(
            
            Services.HohoemaPlaylist hohoemaPlaylist,
            Services.PageManager pageManager,
            ActivityFeedSettings activityFeedSettings,
            Models.Provider.LoginUserNicoRepoProvider loginUserNicoRepoProvider,
            Models.Subscription.SubscriptionManager subscriptionManager
            )
            : base(pageManager, useDefaultPageTitle: true)
        {
            HohoemaPlaylist = hohoemaPlaylist;
            ActivityFeedSettings = activityFeedSettings;
            LoginUserNicoRepoProvider = loginUserNicoRepoProvider;
            SubscriptionManager = subscriptionManager;
            DisplayNicoRepoItemTopics = ActivityFeedSettings.DisplayNicoRepoItemTopics.ToList();

            /*
            DisplayNicoRepoItemTopics.CollectionChangedAsObservable()
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(async _ => 
                {
                    await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => 
                    {
                        await ResetList();
                    });
                });
              */
        }

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

        public IList<NicoRepoItemTopic> DisplayNicoRepoItemTopics { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public ActivityFeedSettings ActivityFeedSettings { get; }
        public Models.Provider.LoginUserNicoRepoProvider LoginUserNicoRepoProvider { get; }
        public Models.Subscription.SubscriptionManager SubscriptionManager { get; }

       
        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            // ニレコポ表示設定をニコレポの設定に書き戻し
            var saveTopics = DisplayNicoRepoItemTopics.Distinct().ToList();
            if (saveTopics.Count == 0)
            {
                saveTopics.Add(NicoRepoItemTopic.NicoVideo_User_Video_Upload);
            }
            ActivityFeedSettings.DisplayNicoRepoItemTopics = DisplayNicoRepoItemTopics.Distinct().ToList();
            ActivityFeedSettings.Save().ConfigureAwait(false);

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        protected override IIncrementalSource<HohoemaListingPageItemBase> GenerateIncrementalSource()
        {
            if (DisplayNicoRepoItemTopics.Count == 0)
            {
                DisplayNicoRepoItemTopics.Add(NicoRepoItemTopic.NicoVideo_User_Video_Upload);
            }
            return new LoginUserNicoRepoTimelineSource(LoginUserNicoRepoProvider, SubscriptionManager, DisplayNicoRepoItemTopics);
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
                this.OptionText = $"{TimelineItem.Program.BeginAt.ToString()} 放送開始";

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

            Description = NicoRepoTimelineVM.ItemTopictypeToDescription(ItemTopic, TimelineItem);
        }


        public NicoRepoTimelineItem TimelineItem { get; private set; }

        public NicoRepoItemTopic ItemTopic { get; private set; }

        public string BroadcasterId => CommunityGlobalId.ToString();
    }

    public class NicoRepoVideoTimeline : VideoInfoControlViewModel
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

            Description = NicoRepoTimelineVM.ItemTopictypeToDescription(ItemTopic, TimelineItem);
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
                    return $"（対応していないニコレポアイテム）";
                    
                case NicoRepoItemTopic.NicoVideo_User_Video_Kiriban_Play:
                    return $"動画再生数がキリ番に到達しました";
                    
                case NicoRepoItemTopic.NicoVideo_User_Video_Upload:
                    return $"{timelineItem.SenderNiconicoUser.Nickname} さんが動画を投稿";
                    
                case NicoRepoItemTopic.NicoVideo_Community_Level_Raise:
                    return $"コミュニティレベル";
                    
                case NicoRepoItemTopic.NicoVideo_User_Mylist_Add_Video:
                    return $"{timelineItem.SenderNiconicoUser.Nickname} さんがマイリストに動画を追加";
                    
                case NicoRepoItemTopic.NicoVideo_User_Community_Video_Add:
                    return $"コミュニティ動画に動画が追加されました";
                    
                case NicoRepoItemTopic.NicoVideo_User_Video_UpdateHighestRankings:
                    return $"動画がランキングにランクイン";
                    
                case NicoRepoItemTopic.NicoVideo_User_Video_Advertise:
                    return $"動画が広告されました";
                    
                case NicoRepoItemTopic.NicoVideo_Channel_Blomaga_Upload:
                    return $"ブロマガが投稿されました";
                    
                case NicoRepoItemTopic.NicoVideo_Channel_Video_Upload:
                    return $"{timelineItem.SenderChannel.Name} が動画を投稿";
                    
                case NicoRepoItemTopic.Live_User_Program_OnAirs:
                    return $"{timelineItem.SenderNiconicoUser.Nickname} さんが生放送を開始";
                    
                case NicoRepoItemTopic.Live_User_Program_Reserve:
                    return $"{timelineItem.SenderNiconicoUser.Nickname} さんが生放送を予約";
                    
                case NicoRepoItemTopic.Live_Channel_Program_Onairs:
                    return $"{timelineItem.SenderChannel.Name} が生放送を開始";
                    
                case NicoRepoItemTopic.Live_Channel_Program_Reserve:
                    return $"{timelineItem.SenderChannel.Name} が生放送を予約";
                    
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

        protected override async Task<IAsyncEnumerable<HohoemaListingPageItemBase>> GetPagedItemsImpl(int head, int count)
        {
            var tail = head + count;
            var prevCount = TimelineItems.Count;
            if (TimelineItems.Count < tail)
            {
                while(prevCount == TimelineItems.Count)
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

            var list = new List<HohoemaListingPageItemBase>();

            return TimelineItems.Skip(head).Take(count).ToArray()
                .Select<NicoRepoTimelineItem, HohoemaListingPageItemBase>(item => 
                {
                    var topicType = NicoRepoItemTopicExtension.ToNicoRepoTopicType(item.Topic);
                    if (topicType == NicoRepoItemTopic.Live_User_Program_OnAirs
                    || topicType == NicoRepoItemTopic.Live_User_Program_Reserve
                    || topicType == NicoRepoItemTopic.Live_Channel_Program_Onairs
                    || topicType == NicoRepoItemTopic.Live_Channel_Program_Reserve)
                    {
                        return new NicoRepoLiveTimeline(item, topicType);
                    }
                    else if (topicType == NicoRepoItemTopic.NicoVideo_User_Video_Upload || 
                            topicType == NicoRepoItemTopic.NicoVideo_User_Mylist_Add_Video || 
                            topicType == NicoRepoItemTopic.NicoVideo_Channel_Video_Upload)
                    {
                        return new NicoRepoVideoTimeline(item, topicType);
                    }
                    else
                    {
                        throw new NotSupportedException(topicType.ToString());
                    }
                })
                .ToAsyncEnumerable();
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
