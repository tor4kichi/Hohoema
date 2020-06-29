using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Search
{
    public enum SearchTargetType
    {
        Title = 0x01,
        Description = 0x02,
        Tags = 0x04,

        All = Title | Description | Tags
    }
}
