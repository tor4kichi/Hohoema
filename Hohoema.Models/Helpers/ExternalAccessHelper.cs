using Hohoema.Models.Repository;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Repository.Niconico.Mylist;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Helpers
{
    public static class ExternalAccessHelper
    {
        public static Uri ConvertToUrl(INiconicoObject content) => content switch
        {
            IUser user => new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.MakeUserPageUrl(user.Id))),
            IVideoContent video => new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.VideoWatchPageUrl, video.Id)),
            IMylist mylist => new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.MakeMylistPageUrl(mylist.Id))),
            IChannel channel => new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.ChannelUrlBase, channel.Id)),
            ICommunity community => new Uri(Path.Combine(Mntone.Nico2.NiconicoUrls.CommynitySammaryPageUrl, community.Id)),
            _ => throw new NotSupportedException(content.GetType().Name)
        };
    }
}
