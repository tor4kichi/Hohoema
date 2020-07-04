﻿using Hohoema.Services;
using Hohoema.Services.Player;
using Hohoema.UseCase.Events;
using Microsoft.Toolkit.Uwp.Helpers;
using Prism.Events;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Hohoema.UseCase.NicoVideoPlayer
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

            DisplayMode = ReadDisplayMode();

            _eventAggregator.GetEvent<PlayerPlayVideoRequest>()
                .Subscribe(async e => 
                {
                    var pageName = nameof(Views.VideoPlayerPage);
                    var parameters = new NavigationParameters("id=" + e.VideoId);
                    
                    await PlayWithCurrentView(DisplayMode, pageName, parameters);
                });

            _eventAggregator.GetEvent<PlayerPlayLiveRequest>()
                .Subscribe(async e =>
                {
                    var pageName = nameof(Views.LivePlayerPage);
                    var parameters = new NavigationParameters("id=" + e.LiveId);
                    
                    await PlayWithCurrentView(DisplayMode, pageName, parameters);
                });


            _eventAggregator.GetEvent<ChangePlayerDisplayViewRequestEvent>()
                .Subscribe(async () =>
                {                     
                    var mode = DisplayMode == PlayerDisplayView.PrimaryView ? PlayerDisplayView.SecondaryView : PlayerDisplayView.PrimaryView;
                    if (_lastNavigatedPageName != null && _lastNavigatedParameters != null)
                    {
                        await PlayWithCurrentView(mode, _lastNavigatedPageName, _lastNavigatedParameters);
                    }

                    DisplayMode = mode;
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
        private CoreDispatcher _dispatcher;

        public void Dispose()
        {
            SaveDisplayMode();
        }


        static LocalObjectStorageHelper _localObjectStorage = new LocalObjectStorageHelper();
        void SaveDisplayMode()
        {
            _localObjectStorage.Save(nameof(PlayerDisplayView), DisplayMode);
        }

        public static PlayerDisplayView ReadDisplayMode()
        {
            return _localObjectStorage.Read<PlayerDisplayView>(nameof(PlayerDisplayView));
        }
    }
}
