using Hohoema.Models.Domain.Playlist;
using Prism.Navigation;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;

namespace Hohoema.Models.UseCase.Niconico.Player
{
    public interface IPlayerView
    {
        string LastNavigatedPageName { get; }
        HohoemaPlaylistPlayer PlaylistPlayer { get; }

        Task ClearVideoPlayerAsync();
        Task CloseAsync();
        Task NavigationAsync(string pageName, INavigationParameters parameters);
        void SetTitle(string title);
        Task ShowAsync();

        ICommand ToggleFullScreenCommand { get; }

        ICommand ToggleCompactOverlayCommand { get; }


    }
}