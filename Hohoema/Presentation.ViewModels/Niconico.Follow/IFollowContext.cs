using CommunityToolkit.Mvvm.Input;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public interface IFollowContext
    {
        RelayCommand AddFollowCommand { get; }
        bool IsFollowing { get; set; }
        bool NowChanging { get; }
        RelayCommand RemoveFollowCommand { get; }
    }
}