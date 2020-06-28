using System.Collections.Generic;
using System.Threading.Tasks;
using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;

namespace Hohoema.Models
{
    internal class ChannelFollowInfoGroup : FollowInfoGroupBaseTemplate<Mntone.Nico2.Users.Follow.ChannelFollowData>
    {
        public ChannelFollowInfoGroup(
            NiconicoSession niconicoSession, 
            Provider.ChannelFollowProvider channelFollowProvider
            )
        {
            NiconicoSession = niconicoSession;
            ChannelFollowProvider = channelFollowProvider;
        }

        public override FollowItemType FollowItemType => FollowItemType.Channel;

        public override uint MaxFollowItemCount 
            => NiconicoSession.IsPremiumAccount ? FollowManager.PREMIUM_FOLLOW_CHANNEL_MAX_COUNT : FollowManager.FOLLOW_CHANNEL_MAX_COUNT;

        public NiconicoSession NiconicoSession { get; }
        public Provider.ChannelFollowProvider ChannelFollowProvider { get; }

        protected override FollowItemInfo ConvertToFollowInfo(ChannelFollowData source)
        {
            return new FollowItemInfo()
            {
                FollowItemType = FollowItemType.Channel,
                Id = source.Id,
                Name = source.Name,
                ThumbnailUrl = source.ThumbnailUrl,
            };
        }

        protected override string FollowSourceToItemId(ChannelFollowData source)
        {
            return source.Id;
        }

        protected override Task<List<ChannelFollowData>> GetFollowSource()
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