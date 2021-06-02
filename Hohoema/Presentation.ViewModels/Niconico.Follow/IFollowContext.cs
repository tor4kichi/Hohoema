using Prism.Commands;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public interface IFollowContext
    {
        DelegateCommand AddFollowCommand { get; }
        bool IsFollowing { get; set; }
        bool NowChanging { get; }
        DelegateCommand RemoveFollowCommand { get; }
    }
}