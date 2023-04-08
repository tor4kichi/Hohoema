using Hohoema.Models.Niconico.Video;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed class SelectionStartCommand : CommandBase
{
    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override void Execute(object parameter)
    {
        var selectionContext = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<VideoItemsSelectionContext>();
        selectionContext.StartSelection(parameter as IVideoContent);
    }
}
