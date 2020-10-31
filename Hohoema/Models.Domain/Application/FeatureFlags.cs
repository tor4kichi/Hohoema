using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Application
{
    public sealed class FeatureFlags : FlagsRepositoryBase
    {
        public bool UseNewCardViewForVideoListView
        {
            get => Read(false);
            set => Save(value);
        }
    }
}
