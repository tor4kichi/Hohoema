using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NicoPlayerHohoema.Models
{
    public interface IPlayableList : Interfaces.IMylist
    {
//        string Id { get; }
//        string Label { get; }

        PlaylistOrigin Origin { get; }
        int SortIndex { get; }
        int Count { get; }

        string ThumnailUrl { get; }

        ReadOnlyObservableCollection<PlaylistItem> PlaylistItems { get; }

        ICommand AddItemCommand { get; }
    }
}
