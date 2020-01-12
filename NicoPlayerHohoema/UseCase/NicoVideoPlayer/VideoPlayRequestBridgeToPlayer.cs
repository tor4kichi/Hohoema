using Microsoft.Toolkit.Uwp.Helpers;
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
    public enum PlayerDisplayView
    {
        PrimaryView,
        SecondaryView,
    }

    public sealed class VideoPlayRequestBridgeToPlayer : IDisposable
    {
        private readonly ScondaryViewPlayerManager _secondaryPlayerManager;
        private readonly PrimaryViewPlayerManager _primaryViewPlayerManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IScheduler _scheduler;


        Models.Helpers.AsyncLock _asyncLock = new Models.Helpers.AsyncLock();

        public VideoPlayRequestBridgeToPlayer(
            ScondaryViewPlayerManager playerViewManager,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            IEventAggregator eventAggregator,
            IScheduler scheduler
            )
        {
            _secondaryPlayerManager = playerViewManager;
            _primaryViewPlayerManager = primaryViewPlayerManager;
            _eventAggregator = eventAggregator;
            _scheduler = scheduler;

            _displayMode = ReadDisplayMode();

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
                .Subscribe(async () =>
                {                     
                    var mode = _displayMode == PlayerDisplayView.PrimaryView ? PlayerDisplayView.SecondaryView : PlayerDisplayView.PrimaryView;
                    if (_lastNavigatedPageName != null && _lastNavigatedParameters != null)
                    {
                        await PlayWithCurrentView(mode, _lastNavigatedPageName, _lastNavigatedParameters);
                    }

                    _displayMode = mode;
                    SaveDisplayMode();
                }
                , ThreadOption.UIThread, true);

            _dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;
        }


        async Task PlayWithCurrentView(PlayerDisplayView displayMode, string pageName, INavigationParameters parameters)
        {
            using (await _asyncLock.LockAsync())
            {
                if (string.IsNullOrEmpty(pageName)) { throw new ArgumentException(nameof(pageName)); }

                if (_lastNavigatedPageName == pageName
                    && (_lastNavigatedParameters?.SequenceEqual(parameters) ?? false))
                {
                    System.Diagnostics.Debug.WriteLine("Navigation skiped. (same page name and parameter)");
                    return;
                }

                if (displayMode == PlayerDisplayView.PrimaryView)
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
        }

        PlayerDisplayView _displayMode;
        string _lastNavigatedPageName;
        INavigationParameters _lastNavigatedParameters;
        private CoreDispatcher _dispatcher;

        public void Dispose()
        {
            SaveDisplayMode();
        }


        LocalObjectStorageHelper _localObjectStorage = new LocalObjectStorageHelper();
        void SaveDisplayMode()
        {
            _localObjectStorage.Save(nameof(PlayerDisplayView), _displayMode);
        }

        PlayerDisplayView ReadDisplayMode()
        {
            return _localObjectStorage.Read<PlayerDisplayView>(nameof(PlayerDisplayView));
        }
    }
}
