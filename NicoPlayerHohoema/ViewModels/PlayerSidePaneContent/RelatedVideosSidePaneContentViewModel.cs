using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PlayerSidePaneContent
{
    public sealed class RelatedVideosSidePaneContentViewModel : SidePaneContentViewModelBase
    {
        public List<VideoInfoControlViewModel> Videos { get; }

        public VideoInfoControlViewModel CurrentVideo { get; }

        public RelatedVideosSidePaneContentViewModel(string currentVideoId, IEnumerable<Database.NicoVideo> videos)
        {
            Videos = videos.Select(x => new VideoInfoControlViewModel(x, requireLatest: false)).ToList();
            CurrentVideo = Videos.FirstOrDefault(x => x.RawVideoId == currentVideoId);
        }
    }
}
