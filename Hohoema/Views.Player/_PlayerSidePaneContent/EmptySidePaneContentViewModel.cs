namespace Hohoema.ViewModels.Player.PlayerSidePaneContent;

public class EmptySidePaneContentViewModel : SidePaneContentViewModelBase
{
    public static EmptySidePaneContentViewModel Default { get; } = new EmptySidePaneContentViewModel();

    private EmptySidePaneContentViewModel() { }
}
