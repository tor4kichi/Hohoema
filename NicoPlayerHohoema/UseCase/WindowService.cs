using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace NicoPlayerHohoema.Services
{
    public sealed class WindowService : BindableBase, IDisposable
    {
        private readonly IScheduler _scheduler;
        private readonly ApplicationView _applicationView;
        CompositeDisposable _disposables = new CompositeDisposable();

        public WindowService(IScheduler scheduler)
        {
            _scheduler = scheduler;

            _applicationView = ApplicationView.GetForCurrentView();

            IsFullScreen = new ReactiveProperty<bool>(_scheduler, _applicationView.IsFullScreenMode, ReactivePropertyMode.DistinctUntilChanged);
            IsFullScreen
                .Subscribe(isFullScreen =>
                {

                    IsCompactOverlay.Value = false;

                    if (isFullScreen)
                    {
                        if (!_applicationView.TryEnterFullScreenMode())
                        {
                            IsFullScreen.Value = false;
                        }
                    }
                    else
                    {
                        _applicationView.ExitFullScreenMode();
                    }
                })
            .AddTo(_disposables);


            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                IsCompactOverlay = new ReactiveProperty<bool>(_scheduler,
                    _applicationView.ViewMode == ApplicationViewMode.CompactOverlay
                    );

                // This device supports all APIs in UniversalApiContract version 2.0
                IsCompactOverlay
                .Subscribe(async isCompactOverlay =>
                {
                    if (_applicationView.IsViewModeSupported(ApplicationViewMode.CompactOverlay))
                    {
                        if (isCompactOverlay)
                        {
                            ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                            compactOptions.CustomSize = new Windows.Foundation.Size(500, 280);

                            var result = await _applicationView.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
                            if (result)
                            {
                                _applicationView.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                                _applicationView.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                            }
                        }
                        else
                        {
                            var result = await _applicationView.TryEnterViewModeAsync(ApplicationViewMode.Default);
                        }
                    }
                })
                .AddTo(_disposables);
            }
            else
            {
                IsCompactOverlay = new ReactiveProperty<bool>(_scheduler, false);
            }
        }


        // Settings
        public ReactiveProperty<bool> IsFullScreen { get; private set; }
        public ReactiveProperty<bool> IsCompactOverlay { get; private set; }

        // TODO
        // CompactOverlay
        // FullScreen
        private DelegateCommand _ToggleFullScreenCommand;
        public DelegateCommand ToggleFullScreenCommand
        {
            get
            {
                return _ToggleFullScreenCommand
                    ?? (_ToggleFullScreenCommand = new DelegateCommand(() =>
                    {
                        IsFullScreen.Value = !IsFullScreen.Value;
                    }
                    ));
            }
        }


        private DelegateCommand _ToggleCompactOverlayCommand;
        public DelegateCommand ToggleCompactOverlayCommand
        {
            get
            {
                return _ToggleCompactOverlayCommand
                    ?? (_ToggleCompactOverlayCommand = new DelegateCommand(() =>
                    {
                        IsCompactOverlay.Value = !IsCompactOverlay.Value;
                    }
                    , () => ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4)
                    ));
            }
        }


        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
