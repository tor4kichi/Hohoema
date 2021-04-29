using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.Player;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.Helpers;
using Prism.Navigation;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Uno.Threading;

namespace Hohoema.Models.UseCase.NicoVideos.Player
{
    public enum PlayerDisplayView
    {
        PrimaryView,
        SecondaryView,
    }

    public sealed class VideoPlayRequestBridgeToPlayer 
        : IDisposable,
        IRecipient<PlayerPlayVideoRequestMessage>,
        IRecipient<PlayerPlayLiveRequestMessage>,
        IRecipient<ChangePlayerDisplayViewRequestMessage>
    {
        private readonly ScondaryViewPlayerManager _secondaryPlayerManager;
        private readonly PrimaryViewPlayerManager _primaryViewPlayerManager;
        

        FastAsyncLock _asyncLock = new FastAsyncLock();

        public VideoPlayRequestBridgeToPlayer(
            ScondaryViewPlayerManager playerViewManager,
            PrimaryViewPlayerManager primaryViewPlayerManager
            )
        {
            _secondaryPlayerManager = playerViewManager;
            _primaryViewPlayerManager = primaryViewPlayerManager;

            DisplayMode = ReadDisplayMode();

            StrongReferenceMessenger.Default.Register<PlayerPlayVideoRequestMessage>(this);
            StrongReferenceMessenger.Default.Register<PlayerPlayLiveRequestMessage>(this);
            StrongReferenceMessenger.Default.Register<ChangePlayerDisplayViewRequestMessage>(this);
        }


        async Task PlayWithCurrentView(PlayerDisplayView displayMode, string pageName, INavigationParameters parameters)
        {
            using (await _asyncLock.LockAsync(default))
            {
                if (string.IsNullOrEmpty(pageName)) { throw new ArgumentException(nameof(pageName)); }

                if (displayMode == PlayerDisplayView.PrimaryView)
                {
                    if (_primaryViewPlayerManager.DisplayMode != PrimaryPlayerDisplayMode.Close)
                    {
                        if (DisplayMode == displayMode
                        && _lastNavigatedPageName == pageName
                        && (_lastNavigatedParameters?.SequenceEqual(parameters) ?? false))
                        {
                            System.Diagnostics.Debug.WriteLine("Navigation skiped. (same page name and parameter)");
                            return;
                        }
                    }

                    await _secondaryPlayerManager.CloseAsync().ConfigureAwait(false);

                    await Task.Delay(10);

                    _ = _primaryViewPlayerManager.NavigationAsync(pageName, parameters);
                }
                else
                {
                    if (DisplayMode == displayMode
                    && _lastNavigatedPageName == pageName
                    && (_lastNavigatedParameters?.SequenceEqual(parameters) ?? false))
                    {
                        System.Diagnostics.Debug.WriteLine("Navigation skiped. (same page name and parameter)");
                        await _secondaryPlayerManager.ShowSecondaryViewAsync();
                        return;
                    }

                    _primaryViewPlayerManager.Close();

                    await Task.Delay(10);

                    _ = _secondaryPlayerManager.NavigationAsync(pageName, parameters).ConfigureAwait(false);
                }

                _lastNavigatedPageName = pageName;
                _lastNavigatedParameters = parameters;
            }
        }

        public PlayerDisplayView DisplayMode { get; private set; }
        string _lastNavigatedPageName;
        INavigationParameters _lastNavigatedParameters;

        public void Dispose()
        {
            SaveDisplayMode();
        }


        static LocalObjectStorageHelper _localObjectStorage = new LocalObjectStorageHelper(new JsonObjectSerializer());
        void SaveDisplayMode()
        {
            _localObjectStorage.Save(nameof(PlayerDisplayView), DisplayMode);
        }

        public static PlayerDisplayView ReadDisplayMode()
        {
            return _localObjectStorage.Read<PlayerDisplayView>(nameof(PlayerDisplayView));
        }

        public async void Receive(PlayerPlayVideoRequestMessage message)
        {
            var pageName = nameof(Presentation.Views.Player.VideoPlayerPage);
            var parameters = new NavigationParameters($"id={message.Value.VideoId}&position={(int)message.Value.Position.TotalSeconds}");

            await PlayWithCurrentView(DisplayMode, pageName, parameters);
        }

        public async void Receive(PlayerPlayLiveRequestMessage message)
        {
            var pageName = nameof(Presentation.Views.Player.LivePlayerPage);
            var parameters = new NavigationParameters("id=" + message.Value.LiveId);

            await PlayWithCurrentView(DisplayMode, pageName, parameters);
        }

        public async void Receive(ChangePlayerDisplayViewRequestMessage message)
        {
            var mode = DisplayMode == PlayerDisplayView.PrimaryView ? PlayerDisplayView.SecondaryView : PlayerDisplayView.PrimaryView;
            if (_lastNavigatedPageName != null && _lastNavigatedParameters != null)
            {
                await PlayWithCurrentView(mode, _lastNavigatedPageName, _lastNavigatedParameters);
            }

            DisplayMode = mode;
            SaveDisplayMode();
        }
    }
}
