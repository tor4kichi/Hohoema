using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Mntone.Nico2.NicoRepo;

namespace Hohoema.Models.Repository.NicoRepo
{
    public sealed class LoginUserNicoRepoProvider : ProviderBase
    {
        public LoginUserNicoRepoProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<NicoRepoResult> GetLoginUserNicoRepo(NicoRepoTimelineType type, NicoRepoResult lastNicoRepoResult = null)
        {
            var res = await ContextActionAsync(context => context.NicoRepo.GetLoginUserNicoRepo(type.To(), lastNicoRepoResult?.LastItemId));

            return new NicoRepoResult(res);
        }
    }

    public enum NicoRepoTimelineType
    {
        All,
        Self,
        FollowingUser,
        FollowingChannel,
        FollowingCommunity,
        FollowingMylist
    }

    public static class NicoRepoTimelineTypeMapper
    {
        public static NicoRepoTimelineType From(this Mntone.Nico2.NicoRepo.NicoRepoTimelineType type) => type switch
        {
            Mntone.Nico2.NicoRepo.NicoRepoTimelineType.all => NicoRepoTimelineType.All,
            Mntone.Nico2.NicoRepo.NicoRepoTimelineType.self => NicoRepoTimelineType.Self,
            Mntone.Nico2.NicoRepo.NicoRepoTimelineType.followingChannel => NicoRepoTimelineType.FollowingChannel,
            Mntone.Nico2.NicoRepo.NicoRepoTimelineType.followingCommunity => NicoRepoTimelineType.FollowingCommunity,
            Mntone.Nico2.NicoRepo.NicoRepoTimelineType.followingMylist => NicoRepoTimelineType.FollowingMylist,
            Mntone.Nico2.NicoRepo.NicoRepoTimelineType.followingUser => NicoRepoTimelineType.FollowingUser,
            _ => throw new NotSupportedException(type.ToString())
        };

        public static Mntone.Nico2.NicoRepo.NicoRepoTimelineType To(this NicoRepoTimelineType type) => type switch
        {
            NicoRepoTimelineType.All => Mntone.Nico2.NicoRepo.NicoRepoTimelineType.all,
            NicoRepoTimelineType.Self => Mntone.Nico2.NicoRepo.NicoRepoTimelineType.self,
            NicoRepoTimelineType.FollowingChannel => Mntone.Nico2.NicoRepo.NicoRepoTimelineType.followingChannel,
            NicoRepoTimelineType.FollowingCommunity => Mntone.Nico2.NicoRepo.NicoRepoTimelineType.followingCommunity,
            NicoRepoTimelineType.FollowingMylist => Mntone.Nico2.NicoRepo.NicoRepoTimelineType.followingMylist,
            NicoRepoTimelineType.FollowingUser => Mntone.Nico2.NicoRepo.NicoRepoTimelineType.followingUser,
            _ => throw new NotSupportedException(type.ToString())
        };
    }
    


    public class NicoRepoResult
    {
        private readonly NicoRepoResponse _res;

        internal string LastItemId => _res.LastTimelineItem?.Id;

        public bool IsOK => _res.IsStatusOK;

        public int Limit => _res.Meta.Limit;

        public ImmutableArray<INicoRepoItem> Items { get; }

        public NicoRepoResult(NicoRepoResponse res)
        {
            List<INicoRepoItem> items = new List<INicoRepoItem>();
            foreach (var item in res.TimelineItems)
            {
                var itemTopic = NicoRepoItemTopicExtension.ToNicoRepoTopicType(item.Topic);
                INicoRepoItem nicorepoItem = itemTopic switch
                {
                    NicoRepoItemTopic.Unknown => new NotSupportedNicoRepoItem(itemTopic, item),
                    NicoRepoItemTopic.NicoVideo_User_Video_Kiriban_Play => new NotSupportedNicoRepoItem(itemTopic, item),
                    NicoRepoItemTopic.NicoVideo_User_Video_Upload => new VideoNicoRepoItem(itemTopic, item),
                    NicoRepoItemTopic.NicoVideo_Community_Level_Raise => new NotSupportedNicoRepoItem(itemTopic, item),
                    NicoRepoItemTopic.NicoVideo_User_Mylist_Add_Video => new VideoNicoRepoItem(itemTopic, item),
                    NicoRepoItemTopic.NicoVideo_User_Community_Video_Add => new VideoNicoRepoItem(itemTopic, item),
                    NicoRepoItemTopic.NicoVideo_User_Video_UpdateHighestRankings => new NotSupportedNicoRepoItem(itemTopic, item),
                    NicoRepoItemTopic.NicoVideo_User_Video_Advertise => new NotSupportedNicoRepoItem(itemTopic, item),
                    NicoRepoItemTopic.NicoVideo_Channel_Blomaga_Upload => new NotSupportedNicoRepoItem(itemTopic, item),
                    NicoRepoItemTopic.NicoVideo_Channel_Video_Upload => new VideoNicoRepoItem(itemTopic, item),
                    _ => null,
                };


                if (nicorepoItem == null) { continue; }

                items.Add(nicorepoItem);
            }

            Items = items.ToImmutableArray();
            _res = res;
        }
    }

    public interface INicoRepoItem
    {
        NicoRepoItemTopic ItemTopic { get; }
        string Id { get; }
        DateTime CreatedAt { get; }
        bool IsVisible { get; }
        bool IsMuted { get; }
        bool? IsDeletable { get; }
    }

    public interface IVideoNicoRepoItem : INicoRepoItem
    {
        string VideoId { get; }

        string VideoStatus { get; }
        
        string VideoSmallThumbnailUrl { get; }
        string VideoNormalThumbnailUrl { get; }

        string Title { get; }

        string VideoWatchPageId { get; }

        VideoProviderType ProviderType { get; }
        string SenderId { get; }
        string SenderName { get; }
    }


    public interface INotSuppotedNicoRepoItem : INicoRepoItem
    {

    }

    internal class VideoNicoRepoItem : IVideoNicoRepoItem
    {
        private readonly NicoRepoTimelineItem _tlItem;

        public NicoRepoItemTopic ItemTopic { get; }

        public string VideoId => _tlItem.Video.Id;

        public string VideoStatus => _tlItem.Video.Status;

        public string VideoSmallThumbnailUrl => _tlItem.Video.ThumbnailUrl.Small;

        public string VideoNormalThumbnailUrl => _tlItem.Video.ThumbnailUrl.Normal;

        public string Title => _tlItem.Video.Title;

        public string VideoWatchPageId => _tlItem.Video.VideoWatchPageId;

        public string Id => _tlItem.Id;

        public DateTime CreatedAt => _tlItem.CreatedAt;

        public bool IsVisible => _tlItem.IsVisible;

        public bool IsMuted => _tlItem.IsMuted;

        public bool? IsDeletable => _tlItem.IsDeletable;

        public VideoProviderType ProviderType => _tlItem.SenderNiconicoUser != null ? VideoProviderType.User : VideoProviderType.Channel;
        public string SenderId => ProviderType switch
        {
            VideoProviderType.User => _tlItem.SenderNiconicoUser.Id.ToString(),
            VideoProviderType.Channel => "ch" + _tlItem.SenderChannel.Id.ToString(),
            _ => throw new NotSupportedException(ProviderType.ToString())
        };

        public string SenderName => ProviderType switch
        {
            VideoProviderType.User => _tlItem.SenderNiconicoUser.Nickname,
            VideoProviderType.Channel => _tlItem.SenderChannel.Name,
            _ => throw new NotSupportedException(ProviderType.ToString())
        };
        public VideoNicoRepoItem(NicoRepoItemTopic topic, NicoRepoTimelineItem tlItem)
        {
            ItemTopic = topic;
            _tlItem = tlItem;
        }

    }


    internal class NotSupportedNicoRepoItem : INotSuppotedNicoRepoItem
    {
        private readonly NicoRepoTimelineItem _tlItem;

        public NicoRepoItemTopic ItemTopic { get; }

        public string Id => _tlItem.Id;

        public DateTime CreatedAt => _tlItem.CreatedAt;

        public bool IsVisible => _tlItem.IsVisible;

        public bool IsMuted => _tlItem.IsMuted;

        public bool? IsDeletable => _tlItem.IsDeletable;

        public NotSupportedNicoRepoItem(NicoRepoItemTopic itemTopic, NicoRepoTimelineItem tlItem)
        {
            ItemTopic = itemTopic;
            _tlItem = tlItem;
        }
    }
}
