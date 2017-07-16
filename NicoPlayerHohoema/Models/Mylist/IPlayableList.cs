using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    public interface IPlayableList
    {
        PlaylistOrigin Origin { get; }
        string Id { get; }
        int SortIndex { get; }
        string Name { get; }

        ReadOnlyObservableCollection<PlaylistItem> PlaylistItems { get; }
    }
}
