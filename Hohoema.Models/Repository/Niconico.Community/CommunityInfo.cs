using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Community
{
    public sealed class CommunityInfo
    {
        private readonly Mntone.Nico2.Communities.Info.CommunityInfo _info;

        public CommunityInfo(Mntone.Nico2.Communities.Info.CommunityInfo info)
        {
            _info = info;
        }

        public string Id => _info.Id;

        public string Name => _info.Name;

        public string Description => _info.Description;

        public string ChannelId => _info.ChannelId;

        public bool IsPublic => _info.IsPublic;
        public bool IsOfficial => _info.IsOfficial;
        public bool IsHidden => _info.IsHidden;

        public string UserId => _info.UserId;

        public string GlobalId => _info.GlobalId;


        public DateTime CreateTime => _info.CreateTime;

        public int UserMax => (int)_info.UserMax;
        public int UserCount => (int)_info.UserCount;

        public int Level => (int)_info.Level;

        public string ThumbnailUrl => _info.Thumbnail;

        public string ThumbnailSmallUrl => _info.ThumbnailSmall;

        public string Type => _info.Type;

        public string TopUrl => _info.TopUrl;

        public string Key => _info.key;

        public string OptionFlags => _info.OptionFlag;

        public string CommunityPrivUserAuth => _info.option_flag_details.CommunityPrivUserAuth;
        public string CommunityIconUpload => _info.option_flag_details.CommunityIconUpload;

        public string AdultFlag => _info.Option.AdultFlag;
        public string AllowDisplayVast => _info.Option.AllowDisplayVast;
    }
}
