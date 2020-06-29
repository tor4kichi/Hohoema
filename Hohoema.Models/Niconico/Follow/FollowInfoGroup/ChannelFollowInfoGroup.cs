using System.Collections.Generic;
using System.Threading.Tasks;
using Hohoema.Models.Repository;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Repository.Niconico.Follow;

namespace Hohoema.Models.Niconico.Follow
{
    public class ChannelFollowInfoGroup : FollowInfoGroupBaseTemplate<Mntone.Nico2.Users.Follow.ChannelFollowData>
    {
        public ChannelFollowInfoGroup(
            NiconicoSession niconicoSession, 
            ChannelFollowProvider channelFollowProvider
            )
        {
            NiconicoSession = niconicoSession;
            ChannelFollowProvider = channelFollowProvider;
        }

        public override FollowItemType FollowItemType => FollowItemType.Channel;

        public override uint MaxFollowItemCount 
            => NiconicoSession.IsPremiumAccount ? FollowManager.PREMIUM_FOLLOW_CHANNEL_MAX_COUNT : FollowManager.FOLLOW_CHANNEL_MAX_COUNT;

        public NiconicoSession NiconicoSession { get; }
        public ChannelFollowProvider ChannelFollowProvider { get; }

        protected override FollowItemInfo ConvertToFollowInfo(Mntone.Nico2.Users.Follow.ChannelFollowData source)
        {
            return new FollowItemInfo()
            {
                FollowItemType = FollowItemType.Channel,
                Id = source.Id,
                Name = source.Name,
                ThumbnailUrl = source.ThumbnailUrl,
            };
        }

        protected override string FollowSourceToItemId(Mntone.Nico2.Users.Follow.ChannelFollowData source)
        {
            return source.Id;
        }

        protected override Task<List<Mntone.Nico2.Users.Follow.ChannelFollowData>> GetFollowSource()
        {
            return ChannelFollowProvider.GetAllAsync();
        }

        protected override async Task<ContentManageResult> AddFollow_Internal(string id, object token = null)
        {
            return await ChannelFollowProvider.AddFollowAsync(id);
        }

        protected override async Task<ContentManageResult> RemoveFollow_Internal(string id)
        {
            return await ChannelFollowProvider.RemoveFollowAsync(id);
        }
    }
}