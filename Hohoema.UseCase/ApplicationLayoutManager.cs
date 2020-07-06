using Hohoema.FixPrism;
using Hohoema.Models.Pages;
using Hohoema.Models.Helpers;
using Hohoema.Models.Repository.App;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Xamarin.Essentials;

namespace Hohoema.UseCase
{
    public sealed class ApplicationLayoutManager : BindableBase
    {
        private readonly AppearanceSettingsRepository _appearanceSettingsRepository;

        private Models.Pages.ApplicationLayout _AppLayout;
        public Models.Pages.ApplicationLayout AppLayout
        {
            get { return _AppLayout; }
            private set { SetProperty(ref _AppLayout, value); }
        }

        private Models.Pages.ApplicationInteractionMode _InteractionMode;
        public Models.Pages.ApplicationInteractionMode InteractionMode
        {
            get { return _InteractionMode; }
            private set { SetProperty(ref _InteractionMode, value); }
        }

        public ApplicationLayoutManager(AppearanceSettingsRepository appearanceSettingsRepository)
        {
            _appearanceSettingsRepository = appearanceSettingsRepository;
            new[]
            {
                _appearanceSettingsRepository.ObserveProperty(x => x.OverrideIntractionMode).ToUnit(),
                Observable.FromEventPattern<WindowSizeChangedEventHandler, WindowSizeChangedEventArgs>(
                    h => Window.Current.SizeChanged += h,
                    h => Window.Current.SizeChanged -= h
                    ).ToUnit()
            }
            .Merge()
            .Subscribe(_ => RefreshAppLayout());
        }

        void RefreshAppLayout()
        {
            InteractionMode = GetInteractionMode();
            AppLayout = GetAppLayout(InteractionMode);
            System.Diagnostics.Debug.WriteLine($"InteractionMode: {InteractionMode}, ApplicationLayout: {AppLayout} (override: {_appearanceSettingsRepository.OverrideIntractionMode.HasValue})");
        }

        Models.Pages.ApplicationInteractionMode GetInteractionMode()
        {
            Models.Pages.ApplicationInteractionMode intaractionMode = ApplicationInteractionMode.Touch;
            if (_appearanceSettingsRepository.OverrideIntractionMode.HasValue)
            {
                intaractionMode = _appearanceSettingsRepository.OverrideIntractionMode.Value;
            }
            else
            {
                if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                {
                    intaractionMode = ApplicationInteractionMode.Mouse;
                }
                else if (DeviceInfo.Idiom == DeviceIdiom.Phone || DeviceInfo.Idiom == DeviceIdiom.Tablet)
                {
                    intaractionMode = ApplicationInteractionMode.Touch;
                }
                else if (DeviceInfo.Idiom == DeviceIdiom.TV || DeviceTypeHelper.IsXbox)
                {
                    intaractionMode = ApplicationInteractionMode.Controller;
                }
            }
            return intaractionMode;
        }

        ApplicationLayout GetAppLayout(ApplicationInteractionMode intaractionMode)
        {
            ApplicationLayout layout = ApplicationLayout.Mobile;
            if (intaractionMode == ApplicationInteractionMode.Mouse
                || intaractionMode == ApplicationInteractionMode.Touch
                )
            {
                var width = Window.Current.Bounds.Width;
                if (width <= 519)
                {
                    layout = ApplicationLayout.Mobile;
                }
                else if (width <= 799)
                {
                    layout = ApplicationLayout.Tablet;
                }
                else
                {
                    layout = ApplicationLayout.Desktop;
                }
            }
            else if (intaractionMode == ApplicationInteractionMode.Controller)
            {
                layout = ApplicationLayout.TV;
            }

            return layout;
        }
    }
}
