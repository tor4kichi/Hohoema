using Hohoema.Models.Application;
using Hohoema.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Playlist
{
    public sealed class QueuePlaylistSetting : FlagsRepositoryBase
    {
        public string LastSelectedSortOptions
        {
            get => Read(string.Empty);
            set => Save(value);
        }


        public bool IsGroupingNearByTitleThenByTitleAscending
        {
            get => Read(true);
            set => Save(value);
        }

        public const double DefaultTitleSimulalityThreshold = 0.8;

        public double TitleSimulalityThreshold
        {
            get => Read(DefaultTitleSimulalityThreshold);
            set => Save(value);
        }

    }
}
