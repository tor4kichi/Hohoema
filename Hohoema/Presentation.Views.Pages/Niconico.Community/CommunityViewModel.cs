using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Presentation.ViewModels.Niconico.Follow;
using NiconicoToolkit;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Community
{
    public sealed class CommunityViewModel : ICommunity
    {
        public NiconicoId CommunityId { get; set; }

        public string Name { get; set; }
    }
}
