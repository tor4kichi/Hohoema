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

        public ApplicationIntaractionMode IntaractionMode { get; private set; }

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
            IntaractionMode = GetInteractionMode();
            AppLayout = GetAppLayout(IntaractionMode);
            System.Diagnostics.Debug.WriteLine($"InteractionMode: {IntaractionMode}, ApplicationLayout: {AppLayout} (override: {_appearanceSettings.OverrideIntractionMode.HasValue})");
        }

        ApplicationIntaractionMode GetInteractionMode()
        {
            ApplicationIntaractionMode intaractionMode = ApplicationIntaractionMode.Touch;
            if (_appearanceSettings.OverrideIntractionMode.HasValue)
            {
                intaractionMode = _appearanceSettings.OverrideIntractionMode.Value;
            }
            else
            {
                if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                {
                    intaractionMode = ApplicationIntaractionMode.Mouse;
                }
                else if (DeviceInfo.Idiom == DeviceIdiom.Phone || DeviceInfo.Idiom == DeviceIdiom.Tablet)
                {
                    intaractionMode = ApplicationIntaractionMode.Touch;
                }
                else if (DeviceInfo.Idiom == DeviceIdiom.TV)
                {
                    intaractionMode = ApplicationIntaractionMode.Controller;
                }
            }
            return intaractionMode;
        }

        ApplicationLayout GetAppLayout(ApplicationIntaractionMode intaractionMode)
        {
            ApplicationLayout layout = ApplicationLayout.Mobile;
            if (intaractionMode == ApplicationIntaractionMode.Mouse)
            {
                var width = Window.Current.Bounds.Width;
                if (width <= 719)
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
            else if (intaractionMode == ApplicationIntaractionMode.Touch)
            {
                var width = Window.Current.Bounds.Width;
                if (width <= 501)
                {
                    layout = ApplicationLayout.Mobile;
                }
                else if (width <= 1280)
                {
                    layout = ApplicationLayout.Tablet;
                }
                else
                {
                    layout = ApplicationLayout.Desktop;
                }
            }
            else if (intaractionMode == ApplicationIntaractionMode.Controller)
            {
                layout = ApplicationLayout.TV;
            }

            return layout;
        }
    }
}
