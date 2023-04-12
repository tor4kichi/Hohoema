#nullable enable
using Hohoema.Models.Playlist;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hohoema.Contracts.Services.Player;

public enum PlayerDisplayMode
{
    Close,
    FillWindow,
    FullScreen,
    WindowInWindow,
    CompactOverlay,
}

public interface IPlayerView : INotifyPropertyChanged
{
    string LastNavigatedPageName { get; }
    HohoemaPlaylistPlayer PlaylistPlayer { get; }

    Task ClearVideoPlayerAsync();
    Task CloseAsync();
    Task NavigationAsync(string pageName, INavigationParameters parameters);
    void SetTitle(string title);
    Task ShowAsync();

    Task<bool> TrySetDisplayModeAsync(PlayerDisplayMode mode);
    Task<PlayerDisplayMode> GetDisplayModeAsync();

    Task<bool> IsWindowFilledScreenAsync();

    ICommand ToggleFullScreenCommand { get; }

    ICommand ToggleCompactOverlayCommand { get; }

    bool IsFullScreen { get; }

    bool IsCompactOverlay { get; }
}