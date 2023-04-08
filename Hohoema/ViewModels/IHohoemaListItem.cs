using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Hohoema.ViewModels;

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

    bool IsVisible { get; }

    List<ActionSet> SecondaryActions { get; }
}

public class ActionSet
{
    public string Title { get; set; }
    public ICommand Command { get; set; }
}
