using System;

namespace Hohoema.Models.Repository.Niconico.Channel
{
    public sealed class ChannelInfo
    {
        private readonly Mntone.Nico2.Channels.Info.ChannelInfo _info;

        public ChannelInfo(Mntone.Nico2.Channels.Info.ChannelInfo info)
        {
            _info = info;
        }
        public int ChannelId => _info.ChannelId;

        public int CategoryId => _info.CategoryId;

        public string Name => _info.Name;

        public string CompanyViewname => _info.CompanyViewname;

        public DateTime OpenTime => _info.ParseOpenTime();

        public DateTime UpdateTime => _info.ParseUpdateTime();

        public string DfpSetting => _info.DfpSetting;

        public string ScreenName => _info.ScreenName;
    }

}
