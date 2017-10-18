using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Mntone.Nico2.NicoRepo;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Helpers;
using Prism.Commands;

namespace NicoPlayerHohoema.ViewModels
{
    public class NicoRepoPageViewModel : HohoemaListingPageViewModelBase<NicoRepoTimelineVM>
    {
        public NicoRepoPageViewModel(HohoemaApp app, PageManager pageManager) 
            : base(app, pageManager, useDefaultPageTitle: true)
        {
        }

        protected override IIncrementalSource<NicoRepoTimelineVM> GenerateIncrementalSource()
        {
            return new LoginUserNicoRepoTimelineSource(HohoemaApp);
        }
    }


    public class NicoRepoLiveTimeline : NicoRepoTimelineVM, Interfaces.ILiveContent
    {
        public NicoRepoLiveTimeline(NicoRepoTimelineItem timelineItem, NicoRepoItemTopic itemType, HohoemaPlaylist playlist) : base(timelineItem, itemType, playlist)
        {
        }

        public string BroadcasterId => OwnerUserId.ToString();
    }

    public class NicoRepoVideoTimeline : NicoRepoTimelineVM, Interfaces.IVideoContent
    {
        public NicoRepoVideoTimeline(NicoRepoTimelineItem timelineItem, NicoRepoItemTopic itemType, HohoemaPlaylist playlist) : base(timelineItem, itemType, playlist)
        {
        }
    }


    public class NicoRepoTimelineVM : HohoemaListingPageItemBase
    {
        public NicoRepoTimelineItem TimelineItem { get; private set; }

        public HohoemaPlaylist HohoemaPlaylist { get; private set; }

        public NicoRepoItemTopic ItemTopic { get; private set; }

        public NicoRepoTimelineVM(NicoRepoTimelineItem timelineItem, NicoRepoItemTopic itemType, HohoemaPlaylist playlist)
        {
            TimelineItem = timelineItem;
            HohoemaPlaylist = playlist;
            ItemTopic = itemType;

            if (TimelineItem.Program != null)
            {
                this.Label = TimelineItem.Program.Title;
                this.ImageUrlsSource.Add(TimelineItem.Program.ThumbnailUrl);
                this.OptionText = $"{TimelineItem.Program.BeginAt.ToString()} 放送開始";

                if (TimelineItem.Community != null)
                {
                    _OwnerUserId = TimelineItem.Community.Id;
                    OwnerUserName = TimelineItem.Community.Name;
                }
                else
                {
                    _OwnerUserId = TimelineItem.SenderChannel.Id.ToString();
                    OwnerUserName = TimelineItem.SenderChannel.Name;
                }
            }
            else if (TimelineItem.Video != null)
            {
                this.Label = TimelineItem.Video.Title;
                if (TimelineItem.Video.ThumbnailUrl.Small != null)
                {
                    this.ImageUrlsSource.Add(TimelineItem.Video.ThumbnailUrl.Small);
                }
                else if (TimelineItem.Video.ThumbnailUrl.Normal != null)
                {
                    this.ImageUrlsSource.Add(TimelineItem.Video.ThumbnailUrl.Normal);
                }
                this.OptionText = $"{TimelineItem.CreatedAt.ToString()}";

                _OwnerUserId = TimelineItem.SenderNiconicoUser.Id.ToString();
                OwnerUserName = TimelineItem.SenderNiconicoUser.Nickname;
            }


            if (TimelineItem.SenderNiconicoUser != null)
            {
                this.Description = this.TimelineItem.SenderNiconicoUser.Nickname;
            }
            else if (TimelineItem.SenderChannel != null)
            {
                this.Description = this.TimelineItem.SenderChannel.Name;
            }

            switch (ItemTopic)
            {
                case NicoRepoItemTopic.Unknown:
                    Description = $"（対応していないニコレポアイテム）";
                    break;
                case NicoRepoItemTopic.NicoVideo_User_Video_Kiriban_Play:
                    Description = $"動画再生数がキリ番に到達しました";
                    break;
                case NicoRepoItemTopic.NicoVideo_User_Video_Upload:
                    Description = $"{this.TimelineItem.SenderNiconicoUser.Nickname} さんが動画を投稿";
                    break;
                case NicoRepoItemTopic.NicoVideo_Community_Level_Raise:
                    Description = $"コミュニティレベル";
                    break;
                case NicoRepoItemTopic.NicoVideo_User_Mylist_Add_Video:
                    Description = $"{this.TimelineItem.SenderNiconicoUser.Nickname} さんがマイリストに動画を追加";
                    break;
                case NicoRepoItemTopic.NicoVideo_User_Community_Video_Add:
                    Description = $"コミュニティ動画に動画が追加されました";
                    break;
                case NicoRepoItemTopic.NicoVideo_User_Video_UpdateHighestRankings:
                    Description = $"動画がランキングにランクイン";
                    break;
                case NicoRepoItemTopic.NicoVideo_User_Video_Advertise:
                    Description = $"動画が広告されました";
                    break;
                case NicoRepoItemTopic.NicoVideo_Channel_Blomaga_Upload:
                    Description = $"ブロマガが投稿されました";
                    break;
                case NicoRepoItemTopic.Live_User_Program_OnAirs:
                    Description = $"{this.TimelineItem.SenderNiconicoUser.Nickname} さんが生放送を開始";
                    break;
                case NicoRepoItemTopic.Live_User_Program_Reserve:
                    Description = $"{this.TimelineItem.SenderNiconicoUser.Nickname} さんが生放送を予約";
                    break;
                case NicoRepoItemTopic.Live_Channel_Program_Onairs:
                    Description = $"{this.TimelineItem.SenderChannel.Name} が生放送を開始";
                    break;
                case NicoRepoItemTopic.Live_Channel_Program_Reserve:
                    Description = $"{this.TimelineItem.SenderChannel.Name} が生放送を予約";
                    break;
                default:
                    break;
            }
        }

        private string _OwnerUserId;
        public string OwnerUserId => _OwnerUserId;

        public string OwnerUserName { get; private set; }

        public string Id => TimelineItem.Video?.Id ?? TimelineItem.Program.Id;

        public IPlayableList Playlist => null;
    }


    public class LoginUserNicoRepoTimelineSource : HohoemaIncrementalSourceBase<NicoRepoTimelineVM>
    {
        public static NicoRepoItemTopic[] LiveItemType => new []
        {
            NicoRepoItemTopic.Live_User_Program_OnAirs,
            NicoRepoItemTopic.Live_Channel_Program_Onairs,
        };

        public static NicoRepoItemTopic[] VideoItemType => new[]
        {
            NicoRepoItemTopic.NicoVideo_User_Video_Upload,
            NicoRepoItemTopic.NicoVideo_User_Mylist_Add_Video,
        };


        List<NicoRepoTimelineItem> TimelineItems { get; } = new List<NicoRepoTimelineItem>();

        HohoemaApp HohoemaApp { get; }

        public LoginUserNicoRepoTimelineSource(HohoemaApp hohoemaApp)
        {
            HohoemaApp = hohoemaApp;
        }

        

        protected override async Task<int> ResetSourceImpl()
        {
            TimelineItems.Clear();

            var nicoRepoResponse = await HohoemaApp.NiconicoContext.NicoRepo.GetLoginUserNicoRepo(NicoRepoTimelineType.all);

            if (nicoRepoResponse.IsStatusOK)
            {
                TimelineItems.AddRange(nicoRepoResponse.TimelineItems);
                return nicoRepoResponse.Meta.Limit;
            }
            else
            {
                return 0;
            }
        }

        DateTime NiconamaDisplayTime = DateTime.Now - TimeSpan.FromHours(6);

        protected override async Task<IEnumerable<NicoRepoTimelineVM>> GetPagedItemsImpl(int head, int count)
        {
            var tail = head + count;
            if (TimelineItems.Count < tail)
            {
                var nicoRepoResponse = await HohoemaApp.NiconicoContext.NicoRepo.GetLoginUserNicoRepo(NicoRepoTimelineType.all, TimelineItems.LastOrDefault()?.Id);
                if (nicoRepoResponse.IsStatusOK)
                {
                    TimelineItems.AddRange(nicoRepoResponse.TimelineItems);
                }
            }

            var list = new List<NicoRepoTimelineVM>();
            foreach (var item in TimelineItems.Skip(head).Take(count).ToArray())
            {
                var topicType = NicoRepoItemTopicExtension.ToNicoRepoTopicType(item.Topic);
                if (LiveItemType.Any(x => x == topicType))
                {
                    if (item.Program != null)
                    {
                        // 放送開始が現時点より６時間以上前の場合には既に終了済みとして表示しない
                        if (item.Program.BeginAt < NiconamaDisplayTime)
                        {
                            continue;
                        }
                    }

                    var vm = new NicoRepoLiveTimeline(item, topicType, HohoemaApp.Playlist);
                    list.Add(vm);
                }
                else if (VideoItemType.Any(x => x == topicType))
                {
                    var vm = new NicoRepoVideoTimeline(item, topicType, HohoemaApp.Playlist);
                    list.Add(vm);
                }
            }

            head += count;

            return list;
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
}
