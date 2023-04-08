#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace Hohoema.ViewModels.Niconico.Follow;

public interface IFollowContext
{
    RelayCommand AddFollowCommand { get; }
    bool IsFollowing { get; set; }
    bool NowChanging { get; }
    RelayCommand RemoveFollowCommand { get; }
}