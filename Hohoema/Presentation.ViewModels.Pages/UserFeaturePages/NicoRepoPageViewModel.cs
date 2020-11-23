using Hohoema.Models.Domain.Helpers;
using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Models.Domain.Niconico.UserFeature;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Presentation.Services.Page;
using Hohoema.Presentation.ViewModels.LivePages.Commands;
using Hohoema.Presentation.ViewModels.Pages.LivePages;
using Hohoema.Presentation.ViewModels.VideoListPage;
using I18NPortable;
using Mntone.Nico2.Live;
using Mntone.Nico2.NicoRepo;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.Extensions;

namespace Hohoema.Presentation.ViewModels.Pages.UserFeaturePages
{
    public class NicoRepoPageViewModel : HohoemaListingPageViewModelBase<INicoRepoItem>, INavigatedAwareAsync
    {
        public NicoRepoPageViewModel(
            IScheduler scheduler,
            ApplicationLayoutManager applicationLayoutManager,
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            NicoRepoSettings activityFeedSettings,
            LoginUserNicoRepoProvider loginUserNicoRepoProvider,
            SubscriptionManager subscriptionManager,
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
            
            NicoRepoType = new ReactiveProperty<NicoRepoType>(Mntone.Nico2.NicoRepo.NicoRepoType.Video);
            NicoRepoDisplayTarget = new ReactiveProperty<NicoRepoDisplayTarget>();

            new[]
            {
                NicoRepoType.ToUnit(),
                NicoRepoDisplayTarget.ToUnit()
            }
            .Merge()
            .Subscribe(_ => 
            {
                var __ = ResetList();
            })
            .AddTo(_CompositeDisposable);
        }

        bool _NicoRepoItemTopicsChanged;

        public ImmutableArray<NicoRepoType> NicoRepoTypeList { get; } = new[]
        {
            Mntone.Nico2.NicoRepo.NicoRepoType.All,
            Mntone.Nico2.NicoRepo.NicoRepoType.Video,
            Mntone.Nico2.NicoRepo.NicoRepoType.Program,
        }
        .ToImmutableArray();


        public ReactiveProperty<NicoRepoType> NicoRepoType { get; }
        public ReactiveProperty<NicoRepoDisplayTarget> NicoRepoDisplayTarget { get; }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public NicoRepoSettings ActivityFeedSettings { get; }
        public LoginUserNicoRepoProvider LoginUserNicoRepoProvider { get; }
        public SubscriptionManager SubscriptionManager { get; }



        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            // ニレコポ表示設定をニコレポの設定に書き戻し
//            ActivityFeedSettings.DisplayNicoRepoItemTopics = DisplayNicoRepoItemTopics.Distinct().ToList();

            base.OnNavigatedFrom(parameters);
        }

        protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
        {
            /*
            if (!ActivityFeedSettings.DisplayNicoRepoItemTopics.All(x => DisplayNicoRepoItemTopics.Any(y => x == y)))
            {
                DisplayNicoRepoItemTopics.Clear();
                DisplayNicoRepoItemTopics.AddRange(ActivityFeedSettings.DisplayNicoRepoItemTopics);

                _NicoRepoItemTopicsChanged = false;
                return true;
            }
            */
            
            return base.CheckNeedUpdateOnNavigateTo(mode);
        }

        protected override IIncrementalSource<INicoRepoItem> GenerateIncrementalSource()
        {
            return new LoginUserNicoRepoTimelineSource(LoginUserNicoRepoProvider, SubscriptionManager, NicoRepoType.Value, NicoRepoDisplayTarget.Value);
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
    }


    public class NicoRepoLiveTimeline : LiveInfoListItemViewModel, ILiveContent, INicoRepoItem
    {
        private readonly NicoRepoEntry _nicoRepoEntry;

        public NicoRepoLiveTimeline(NicoRepoEntry nicoRepoEntry, NicoRepoItemTopic itemTopic) 
            : base(nicoRepoEntry.GetContentId())
        {
            _nicoRepoEntry = nicoRepoEntry;
            ItemTopic = itemTopic;
            this.Label = nicoRepoEntry.Object.Name;
            this.OptionText = "LiveStreamingStartAtWithDateTime".Translate(_nicoRepoEntry.Updated.ToString());

            ThumbnailUrl = _nicoRepoEntry.Object.Image.OriginalString;
            CommunityThumbnail = _nicoRepoEntry.Actor.Icon.OriginalString;
            CommunityGlobalId = _nicoRepoEntry.MuteContext.Sender.Id;
            CommunityName = _nicoRepoEntry.Actor.Name;

            this.CommunityType = _nicoRepoEntry.MuteContext.Sender.IdType switch
            {
                SenderIdTypeEnum.User => CommunityType.Community,
                SenderIdTypeEnum.Channel => CommunityType.Channel,
            };

            ItempTopicDescription = NicoRepoTimelineVM.ItemTopictypeToDescription(ItemTopic, _nicoRepoEntry);
        }

        public string ThumbnailUrl { get; set; }

        public string ItempTopicDescription { get; }

        public NicoRepoItemTopic ItemTopic { get; private set; }

        public string BroadcasterId => CommunityGlobalId.ToString();
    }

    public interface INicoRepoItem
    {
        NicoRepoItemTopic ItemTopic { get; }
    }

    public class NicoRepoVideoTimeline : VideoInfoControlViewModel, IVideoContent, INicoRepoItem
    {
        private readonly NicoRepoEntry _nicoRepoEntry;

        public NicoRepoVideoTimeline(NicoRepoEntry nicoRepoEntry, NicoRepoItemTopic itemType) 
            : base(nicoRepoEntry.GetContentId())
        {
            _nicoRepoEntry = nicoRepoEntry;
            ItemTopic = itemType;

            Label = _nicoRepoEntry.Object.Name;
            ThumbnailUrl = _nicoRepoEntry.Object.Image.OriginalString;

            ItempTopicDescription = NicoRepoTimelineVM.ItemTopictypeToDescription(ItemTopic, _nicoRepoEntry);
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

        public NicoRepoItemTopic ItemTopic { get; private set; }
    }


    public static class NicoRepoTimelineVM 
    {
      
        public static string ItemTopictypeToDescription(NicoRepoItemTopic topicType, NicoRepoEntry timelineItem)
        {
            switch (topicType)
            {
                case NicoRepoItemTopic.Unknown:
                    return "Unknown".Translate();
                case NicoRepoItemTopic.NicoVideo_User_Video_Upload:
                    return "NicoRepo_Video_UserVideoUpload".Translate(timelineItem.Actor.Name);
                case NicoRepoItemTopic.NicoVideo_User_Mylist_Add_Video:
                    return "NicoRepo_Video_UserMylistAddVideo".Translate(timelineItem.Actor.Name);
                case NicoRepoItemTopic.NicoVideo_User_Community_Video_Add:
                    return "NicoRepo_Video_CommunityAddVideo".Translate(timelineItem.Actor.Name);
                case NicoRepoItemTopic.NicoVideo_Channel_Video_Upload:
                    return "NicoRepo_Video_ChannelVideoUpload".Translate(timelineItem.Actor.Name);
                case NicoRepoItemTopic.Live_User_Program_OnAirs:
                    return "NicoRepo_Live_UserProgramOnAirs".Translate(timelineItem.Actor.Name);
                case NicoRepoItemTopic.Live_User_Program_Reserve:
                    return "NicoRepo_Live_UserProgramReserve".Translate(timelineItem.Actor.Name);
                case NicoRepoItemTopic.Live_Channel_Program_Onairs:
                    return "NicoRepo_Live_ChannelProgramOnAirs".Translate(timelineItem.Actor.Name);
                case NicoRepoItemTopic.Live_Channel_Program_Reserve:
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


    public class LoginUserNicoRepoTimelineSource : HohoemaIncrementalSourceBase<INicoRepoItem>
    {
        // 通常10だが、ニコレポの表示フィルタを掛けた場合に
        // 追加読み込み時に表示対象が見つからない場合
        // 追加読み込みが途絶えるため、多めに設定している
        public override uint OneTimeLoadCount => 25;

        public LoginUserNicoRepoProvider LoginUserNicoRepoProvider { get; }
        public SubscriptionManager SubscriptionManager { get; }

        public LoginUserNicoRepoTimelineSource(
            LoginUserNicoRepoProvider loginUserNicoRepoProvider,
            SubscriptionManager subscriptionManager,
            NicoRepoType nicoRepoType,
            NicoRepoDisplayTarget nicoRepoTarget
            )
        {
            LoginUserNicoRepoProvider = loginUserNicoRepoProvider;
            SubscriptionManager = subscriptionManager;
            _nicoRepoType = nicoRepoType;
            _nicoRepoDisplayTarget = nicoRepoTarget;
        }


        NicoRepoEntriesResponse _firstRes;

        protected override async Task<int> ResetSourceImpl()
        {
            var nicoRepoResponse = await LoginUserNicoRepoProvider.GetLoginUserNicoRepoAsync(_nicoRepoType, _nicoRepoDisplayTarget);

            if (nicoRepoResponse.Data?.Any() ?? false)
            {
                _firstRes = nicoRepoResponse;
                return 500;
            }
            else
            {
                return 500;
            }
        }


        DateTime NiconamaDisplayTime = DateTime.Now - TimeSpan.FromHours(6);
        private readonly NicoRepoType _nicoRepoType;
        private readonly NicoRepoDisplayTarget _nicoRepoDisplayTarget;

        protected override async IAsyncEnumerable<INicoRepoItem> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            NicoRepoEntriesResponse nicoRepoResponse;
            if (head == 0 && _firstRes != null)
            {
                nicoRepoResponse = _firstRes;
            }
            else
            {
                nicoRepoResponse = await LoginUserNicoRepoProvider.GetLoginUserNicoRepoAsync(_nicoRepoType, _nicoRepoDisplayTarget);
            }

            ct.ThrowIfCancellationRequested();

            if (nicoRepoResponse.Meta.Status != 200) { yield break; }

#if DEBUG
            List<string> triggers = new List<string>();
#endif
            foreach (var item in nicoRepoResponse.Data)
            {
#if DEBUG
                triggers.AddDistinct(item.MuteContext.Trigger);
#endif

                var topicType = NicoRepoItemTopicExtension.ToNicoRepoTopicType(item.MuteContext.Trigger);
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
                    var vm = new NicoRepoVideoTimeline(item, topicType);
                    
                    yield return vm;

                    _ = vm.InitializeAsync(ct);
                }
                else
                {
                    //throw new NotSupportedException(topicType.ToString());
                }

                ct.ThrowIfCancellationRequested();
            }

#if DEBUG
            Debug.WriteLine(string.Join(" ", triggers));
#endif
        }

    }

    public static class NicoRepoItemTopicExtension
    {
        public static NicoRepoItemTopic ToNicoRepoTopicType(string topic)
        {
            NicoRepoItemTopic topicType = NicoRepoItemTopic.Unknown;
            switch (topic)
            {
                case "video.nicovideo_user_video_upload":
                    topicType = NicoRepoItemTopic.NicoVideo_User_Video_Upload;
                    break;
                case "program.live_user_program_onairs":
                    topicType = NicoRepoItemTopic.Live_User_Program_OnAirs;
                    break;


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
