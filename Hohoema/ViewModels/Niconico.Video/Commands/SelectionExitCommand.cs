namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed class SelectionExitCommand : CommandBase
{
    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override void Execute(object parameter)
    {
        var selectionContext = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<VideoItemsSelectionContext>();
        selectionContext.EndSelectioin();
    }
}
