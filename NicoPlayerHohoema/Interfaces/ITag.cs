using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Interfaces
{
    public interface ITag
    {
        string Tag { get; }
        bool IsLocked { get; }
        bool IsCategoryTag { get; }
    }
}
