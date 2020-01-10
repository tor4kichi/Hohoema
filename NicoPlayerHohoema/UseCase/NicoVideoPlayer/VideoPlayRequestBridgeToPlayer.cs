using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Player;
using NicoPlayerHohoema.UseCase.Playlist;
using Prism.Events;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace NicoPlayerHohoema.UseCase.NicoVideoPlayer
{
    public sealed class VideoPlayRequestBridgeToPlayer : IDisposable
    {
        private readonly ScondaryViewPlayerManager _secondaryPlayerManager;
        private readonly PrimaryViewPlayerManager _primaryViewPlayerManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly PlayerSettings _playerSettings;
        private readonly IScheduler _scheduler;

        public VideoPlayRequestBridgeToPlayer(
            ScondaryViewPlayerManager playerViewManager,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            IEventAggregator eventAggregator,
            PlayerSettings playerSettings,
            IScheduler scheduler
            )
        {
            _secondaryPlayerManager = playerViewManager;
            _primaryViewPlayerManager = primaryViewPlayerManager;
            _eventAggregator = eventAggregator;
            _playerSettings = playerSettings;
            _scheduler = scheduler;

            _displayMode = _playerSettings.DisplayMode;

            _eventAggregator.GetEvent<Services.Player.PlayerPlayVideoRequest>()
                .Subscribe(async e => 
                {
                    var pageName = nameof(Views.VideoPlayerPage);
                    var parameters = new NavigationParameters("id=" + e.VideoId);
                    
                    await PlayWithCurrentView(_displayMode, pageName, parameters);
                });

            _eventAggregator.GetEvent<Services.Player.PlayerPlayLiveRequest>()
                .Subscribe(async e =>
                {
                    var pageName = nameof(Views.LivePlayerPage);
                    var parameters = new NavigationParameters("id=" + e.LiveId);

                    await PlayWithCurrentView(_displayMode, pageName, parameters);
                });


            _eventAggregator.GetEvent<ChangePlayerDisplayViewRequestEvent>()
                .Subscribe(async mode => 
                {
                    mode = _displayMode == PlayerDisplayMode.MainWindow ? PlayerDisplayMode.Standalone : PlayerDisplayMode.MainWindow;
                    if (_lastNavigatedPageName != null && _lastNavigatedParameters != null)
                    {
                        await PlayWithCurrentView(mode, _lastNavigatedPageName, _lastNavigatedParameters);
                    }

                    _displayMode = mode;
                }
                , ThreadOption.UIThread, true);

            _dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;
        }


        async Task PlayWithCurrentView(Models.PlayerDisplayMode displayMode, string pageName, INavigationParameters parameters)
        {
            if (displayMode == Models.PlayerDisplayMode.MainWindow)
            {
                await _secondaryPlayerManager.CloseAsync().ConfigureAwait(false);

                await Task.Delay(10);

                _ = _primaryViewPlayerManager.NavigationAsync(pageName, parameters);
            }
            else
            {
                _primaryViewPlayerManager.Close();

                await Task.Delay(10);

                _ = _secondaryPlayerManager.NavigationAsync(pageName, parameters).ConfigureAwait(false);
            }

            _lastNavigatedPageName = pageName;
            _lastNavigatedParameters = parameters;
        }

        Models.PlayerDisplayMode _displayMode;
        string _lastNavigatedPageName;
        INavigationParameters _lastNavigatedParameters;
        private CoreDispatcher _dispatcher;

        public void Dispose()
        {
            _playerSettings.DisplayMode = _displayMode;
        }
    }
}
