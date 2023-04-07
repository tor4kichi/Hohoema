using Hohoema.Models.Niconico.Community;
using Hohoema.ViewModels.Niconico.Follow;
using NiconicoToolkit;
using NiconicoToolkit.Community;

namespace Hohoema.ViewModels.Pages.Niconico.Community
{
    public sealed class CommunityViewModel : ICommunity
    {
        public CommunityId CommunityId { get; set; }

        public string Name { get; set; }
    }
}
