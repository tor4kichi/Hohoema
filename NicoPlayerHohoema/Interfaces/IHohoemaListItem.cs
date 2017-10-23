using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI;

namespace NicoPlayerHohoema.Interfaces
{
    public interface IHohoemaListItem
    {
        string Label { get; }
        bool HasTitle { get; }
        string Description { get; }
        bool HasDescription { get; }
        string OptionText { get; }
        bool HasOptionText { get; }

        ReadOnlyObservableCollection<string> ImageUrls { get; }

        string FirstImageUrl { get; }
        bool HasImageUrl { get; }
        bool IsMultipulImages { get; }

        string ImageCaption { get; }
        bool HasImageCaption { get; }

        Color ThemeColor { get; }

        bool IsVisible { get; }

        List<ActionSet> SecondaryActions { get; }
    }

    public class ActionSet
    {
        public string Title { get; set; }
        public ICommand Command { get; set; }
    }
}
