using System.Collections.Generic;
using System.Threading.Tasks;
using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;

namespace NicoPlayerHohoema.Models
{
    internal class ChannelFollowInfoGroup : FollowInfoGroupBaseTemplate<Mntone.Nico2.Users.Follow.ChannelFollowData>
    {
        private HohoemaApp _HohoemaApp;

        public ChannelFollowInfoGroup(HohoemaApp hohoemaApp)
            : base(hohoemaApp)
        {
            _HohoemaApp = hohoemaApp;
        }

        public override FollowItemType FollowItemType => FollowItemType.Channel;

        public override uint MaxFollowItemCount 
            => HohoemaApp.IsPremiumUser ? FollowManager.PREMIUM_FOLLOW_CHANNEL_MAX_COUNT : FollowManager.FOLLOW_CHANNEL_MAX_COUNT;


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
            return HohoemaApp.ContentProvider.GetFavChannels();
        }

        protected override async Task<ContentManageResult> AddFollow_Internal(string id, object token = null)
        {
            var result = await HohoemaApp.NiconicoContext.User.AddFollowChannelAsync(id);
            return result.IsSucceed ? ContentManageResult.Success : ContentManageResult.Failed;
        }

        protected override async Task<ContentManageResult> RemoveFollow_Internal(string id)
        {
            var result = await HohoemaApp.NiconicoContext.User.DeleteFollowChannelAsync(id);
            return result.IsSucceed ? ContentManageResult.Success : ContentManageResult.Failed;
        }
    }
}