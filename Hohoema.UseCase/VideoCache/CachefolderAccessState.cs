using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.VideoCache
{
    public enum CacheFolderAccessState
    {
        NotAccepted,
        NotEnabled,
        NotSelected,
        SelectedButNotExist,
        Exist
    }
}
