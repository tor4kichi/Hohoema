using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Hohoema.Models;
using Hohoema.Models.Helpers;
using Prism.Commands;
using Reactive.Bindings;
using System.Collections.ObjectModel;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Hohoema.Interfaces;
using Hohoema.Services;
using Prism.Navigation;
using Hohoema.ViewModels.Pages;
using Hohoema.UseCase.Playlist;
using Hohoema.UseCase;
using I18NPortable;
using System.Reactive.Concurrency;
using System.Threading;
using System.Runtime.CompilerServices;
using Hohoema.Models.Repository.NicoRepo;
using Hohoema.Models.Repository;

namespace Hohoema.ViewModels
{
    public class NicoRepoPageViewModel : HohoemaListingPageViewModelBase<HohoemaListingPageItemBase>, INavigatedAwareAsync
    {
        public NicoRepoPageViewModel(
            IScheduler scheduler,
            ApplicationLayoutManager applicationLayoutManager,
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            NicoRepoSettingsRepository nicoRepoSettingsRepository,
            LoginUserNicoRepoProvider loginUserNicoRepoProvider
            )
        {
            _scheduler = scheduler;
            ApplicationLayoutManager = applicationLayoutManager;
            HohoemaPlaylist = hohoemaPlaylist;
            _nicoRepoSettingsRepository = nicoRepoSettingsRepository;
            LoginUserNicoRepoProvider = loginUserNicoRepoProvider;
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
        public NicoRepoSettingsRepository ActivityFeedSettings { get; }
        public LoginUserNicoRepoProvider LoginUserNicoRepoProvider { get; }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            // ニレコポ表示設定をニコレポの設定に書き戻し
            var saveTopics = DisplayNicoRepoItemTopics.Distinct().ToArray();
            if (saveTopics.Length == 0)
            {
                saveTopics = new NicoRepoItemTopic[] { NicoRepoItemTopic.NicoVideo_User_Video_Upload };
                DisplayNicoRepoItemTopics.Add(NicoRepoItemTopic.NicoVideo_User_Video_Upload);
            }

            _nicoRepoSettingsRepository.DisplayNicoRepoItemTopics = saveTopics;

            base.OnNavigatedFrom(parameters);
        }

        protected override IIncrementalSource<HohoemaListingPageItemBase> GenerateIncrementalSource()
        {
            if (DisplayNicoRepoItemTopics.Count == 0)
            {
                DisplayNicoRepoItemTopics.Add(NicoRepoItemTopic.NicoVideo_User_Video_Upload);
            }
            return new LoginUserNicoRepoTimelineSource(LoginUserNicoRepoProvider, DisplayNicoRepoItemTopics);
        }


        DelegateCommand<object> _openNicoRepoItemCommand;
        private readonly IScheduler _scheduler;
        private readonly NicoRepoSettingsRepository _nicoRepoSettingsRepository;
        
        public DelegateCommand<object> OpenNicoRepoItemCommand => _openNicoRepoItemCommand
            ?? (_openNicoRepoItemCommand = new DelegateCommand<object>(item => 
            {
                if (item is NicoRepoVideoTimeline videoItem)
                {
                    HohoemaPlaylist.Play(videoItem);
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


    public class NicoRepoVideoTimeline : VideoInfoControlViewModel, IVideoContent
    {
        public NicoRepoVideoTimeline(IVideoNicoRepoItem timelineItem, NicoRepoItemTopic itemType) 
            : base(timelineItem.VideoId)
        {
            TimelineItem = timelineItem;
            ItemTopic = itemType;

            this.Label = TimelineItem.Title;
            if (TimelineItem.VideoSmallThumbnailUrl!= null)
            {
                AddImageUrl(TimelineItem.VideoSmallThumbnailUrl);
            }
            else if (TimelineItem.VideoNormalThumbnailUrl != null)
            {
                AddImageUrl(TimelineItem.VideoNormalThumbnailUrl);
            }

            this.OptionText = $"{TimelineItem.CreatedAt.ToString()}";

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

        public IVideoNicoRepoItem TimelineItem { get; private set; }

        public NicoRepoItemTopic ItemTopic { get; private set; }
    }


    public static class NicoRepoTimelineVM 
    {
      
        public static string ItemTopictypeToDescription(NicoRepoItemTopic topicType, INicoRepoItem timelineItem)
        {
            switch (topicType)
            {
                case NicoRepoItemTopic.Unknown:
                    return "Unknown".Translate();
                case NicoRepoItemTopic.NicoVideo_User_Video_Upload:
                    return "NicoRepo_Video_UserVideoUpload".Translate((timelineItem as IVideoNicoRepoItem).SenderName);
                case NicoRepoItemTopic.NicoVideo_User_Mylist_Add_Video:
                    return "NicoRepo_Video_UserMylistAddVideo".Translate((timelineItem as IVideoNicoRepoItem).SenderName);
                case NicoRepoItemTopic.NicoVideo_Channel_Video_Upload:
                    return "NicoRepo_Video_ChannelVideoUpload".Translate((timelineItem as IVideoNicoRepoItem).SenderName);
                case NicoRepoItemTopic.Live_User_Program_OnAirs:
                    return "NicoRepo_Live_UserProgramOnAirs".Translate((timelineItem as ILiveNicoRepoItem).ProviderName);
                case NicoRepoItemTopic.Live_User_Program_Reserve:
                    return "NicoRepo_Live_UserProgramReserve".Translate((timelineItem as ILiveNicoRepoItem).ProviderName);
                case NicoRepoItemTopic.Live_Channel_Program_Onairs:
                    return "NicoRepo_Live_ChannelProgramOnAirs".Translate((timelineItem as ILiveNicoRepoItem).ProviderName);
                case NicoRepoItemTopic.Live_Channel_Program_Reserve:
                    return "NicoRepo_Live_ChannelProgramReserve".Translate((timelineItem as ILiveNicoRepoItem).ProviderName);
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
        private readonly List<NicoRepoItemTopic> AllowedNicoRepoItemType;



        NicoRepoResult _nicoRepoResult;

        // 通常10だが、ニコレポの表示フィルタを掛けた場合に
        // 追加読み込み時に表示対象が見つからない場合
        // 追加読み込みが途絶えるため、多めに設定している
        public override uint OneTimeLoadCount => 10;

        public LoginUserNicoRepoProvider LoginUserNicoRepoProvider { get; }

        public LoginUserNicoRepoTimelineSource(
            LoginUserNicoRepoProvider loginUserNicoRepoProvider,
            IEnumerable<NicoRepoItemTopic> allowedNicoRepoTypes
            )
        {
            AllowedNicoRepoItemType = allowedNicoRepoTypes.ToList();
            LoginUserNicoRepoProvider = loginUserNicoRepoProvider;
        }

        List<INicoRepoItem> TimelineItems { get; } = new List<INicoRepoItem>();


        protected override async Task<int> ResetSourceImpl()
        {
            var nicoRepoResponse = await LoginUserNicoRepoProvider.GetLoginUserNicoRepo(NicoRepoTimelineType.All);

            if (nicoRepoResponse.IsOK)
            {
                _nicoRepoResult = nicoRepoResponse;

                foreach (var item in nicoRepoResponse.Items)
                {
                    if (CheckCanDisplayTimelineItem(item))
                    {
                        TimelineItems.Add(item);
                    }
                }

                return nicoRepoResponse.Limit;
            }
            else
            {
                return 0;
            }
        }


        private bool CheckCanDisplayTimelineItem(INicoRepoItem item)
        {
            var topicType = item.ItemTopic;
            if (!AllowedNicoRepoItemType.Any(x => x == topicType))
            {
                return false;
            }

            if (topicType == NicoRepoItemTopic.Live_User_Program_OnAirs
            || topicType == NicoRepoItemTopic.Live_User_Program_Reserve
            || topicType == NicoRepoItemTopic.Live_Channel_Program_Onairs
            || topicType == NicoRepoItemTopic.Live_Channel_Program_Reserve)
            {
                if (item is ILiveNicoRepoItem live)
                {
                    // 放送開始が現時点より６時間以上前の場合には既に終了済みとして表示しない
                    if (live.ProgramBeginAt < NiconamaDisplayTime)
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
                    var nicoRepoResponse = await LoginUserNicoRepoProvider.GetLoginUserNicoRepo(NicoRepoTimelineType.All, _nicoRepoResult);
                    _nicoRepoResult = nicoRepoResponse;
                    if (nicoRepoResponse.IsOK)
                    {
                        foreach (var item in nicoRepoResponse.Items)
                        {
                            if (CheckCanDisplayTimelineItem(item))
                            {
                                TimelineItems.Add(item);
                            }
                        }

                        if (nicoRepoResponse.Items.Length == 0)
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
                var topicType = item.ItemTopic;
                if (item is IVideoNicoRepoItem videoNicoRepoItem)
                {
                    var videoItem = new NicoRepoVideoTimeline(videoNicoRepoItem, topicType);
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



   
    

    public class SelectableNicoRepoItemTopic
    {
        public NicoRepoItemTopic Topic { get; set; }
    }
}
