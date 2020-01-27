using NicoPlayerHohoema.FixPrism;
using NicoPlayerHohoema.Models;
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

namespace NicoPlayerHohoema.UseCase
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public sealed class ApplicationLayoutManager : BindableBase
    {
        private readonly AppearanceSettings _appearanceSettings;

        public ApplicationLayout AppLayout { get; private set; }

        public ApplicationInteractionMode InteractionMode { get; private set; }

        public ApplicationLayoutManager(AppearanceSettings appearanceSettings)
        {
            _appearanceSettings = appearanceSettings;

            new[]
            {
                _appearanceSettings.ObserveProperty(x => x.OverrideIntractionMode).ToUnit(),
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
            System.Diagnostics.Debug.WriteLine($"InteractionMode: {InteractionMode}, ApplicationLayout: {AppLayout} (override: {_appearanceSettings.OverrideIntractionMode.HasValue})");
        }

        ApplicationInteractionMode GetInteractionMode()
        {
            ApplicationInteractionMode intaractionMode = ApplicationInteractionMode.Touch;
            if (_appearanceSettings.OverrideIntractionMode.HasValue)
            {
                intaractionMode = _appearanceSettings.OverrideIntractionMode.Value;
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
                else if (DeviceInfo.Idiom == DeviceIdiom.TV)
                {
                    intaractionMode = ApplicationInteractionMode.Controller;
                }
            }
            return intaractionMode;
        }

        ApplicationLayout GetAppLayout(ApplicationInteractionMode intaractionMode)
        {
            ApplicationLayout layout = ApplicationLayout.Mobile;
            if (intaractionMode == ApplicationInteractionMode.Mouse)
            {
                var width = Window.Current.Bounds.Width;
                if (width <= 519)
                {
                    layout = ApplicationLayout.Tablet;
                }
                else if (width <= 1439)
                {
                    layout = ApplicationLayout.Desktop;
                }
                else
                {
                    layout = ApplicationLayout.TV;
                }
            }
            else if (intaractionMode == ApplicationInteractionMode.Touch)
            {
                var width = Window.Current.Bounds.Width;
                if (width <= 519)
                {
                    layout = ApplicationLayout.Mobile;
                }
                else if (width <= 1039)
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
