using System.Collections.Generic;
using System.Threading.Tasks;
using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;

namespace Hohoema.Models.Domain.Niconico.LoginUser.Follow
{
    internal class ChannelFollowInfoGroup : FollowInfoGroupBaseTemplate<FollowChannelResponse.FollowChannel>
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

        protected override FollowItemInfo ConvertToFollowInfo(FollowChannelResponse.FollowChannel source)
        {
            return new FollowItemInfo()
            {
                FollowItemType = FollowItemType.Channel,
                Id = source.Id.ToString(),
                Name = source.Name,
                ThumbnailUrl = source.ThumbnailUrl.OriginalString,
            };
        }

        protected override string FollowSourceToItemId(FollowChannelResponse.FollowChannel source)
        {
            return source.Id.ToString();
        }

        protected override Task<List<FollowChannelResponse.FollowChannel>> GetFollowSource()
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