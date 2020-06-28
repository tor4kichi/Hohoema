using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Interfaces
{
    public interface IWatchHistory
    {
        string RemoveToken { get; set; }
        string ItemId { get; set; }
    }
}
