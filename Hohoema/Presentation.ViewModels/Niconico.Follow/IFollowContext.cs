using Prism.Commands;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public interface IFollowContext
    {
        DelegateCommand AddFollowCommand { get; }
        bool IsFollowing { get; }
        bool NowChanging { get; }
        DelegateCommand RemoveFollowCommand { get; }
        DelegateCommand ToggleFollowCommand { get; }
    }
}