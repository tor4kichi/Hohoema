﻿using Hohoema.Models.Domain.Playlist;
using Prism.Navigation;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;

namespace Hohoema.Models.UseCase.Niconico.Player
{
    public interface IPlayerView : INotifyPropertyChanged
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

        bool IsFullScreen { get; }

        bool IsCompactOverlay { get; }
    }
}