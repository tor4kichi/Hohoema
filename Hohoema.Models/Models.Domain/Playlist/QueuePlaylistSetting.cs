using Hohoema.Models.Domain.Application;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Playlist
{
    public sealed class QueuePlaylistSetting : FlagsRepositoryBase
    {
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
