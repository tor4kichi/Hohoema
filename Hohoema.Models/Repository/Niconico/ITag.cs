using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository
{
    public interface ITag
    {
        string Tag { get; }
        bool IsLocked { get; }
        bool IsCategoryTag { get; }
    }
}
