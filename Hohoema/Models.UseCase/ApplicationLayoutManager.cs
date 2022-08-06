﻿using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Application;
using Hohoema.Models.Helpers;
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

namespace Hohoema.Models.UseCase
{
    public sealed class ApplicationLayoutManager : ObservableObject
    {
        private readonly AppearanceSettings _appearanceSettings;

        private ApplicationLayout _appLayout;
        public ApplicationLayout AppLayout
        {
            get { return _appLayout; }
            private set { SetProperty(ref _appLayout, value); }
        }

        private ApplicationInteractionMode _InteractionMode;
        public ApplicationInteractionMode InteractionMode
        {
            get { return _InteractionMode; }
            private set { SetProperty(ref _InteractionMode, value); }
        }

        public ApplicationLayoutManager(AppearanceSettings appearanceSettings)
        {
            _appearanceSettings = appearanceSettings;

            new[]
            {
                _appearanceSettings.ObserveProperty(x => x.OverrideInteractionMode).ToUnit(),
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
            System.Diagnostics.Debug.WriteLine($"InteractionMode: {InteractionMode}, ApplicationLayout: {AppLayout} (override: {_appearanceSettings.OverrideInteractionMode.HasValue})");
        }

        ApplicationInteractionMode GetInteractionMode()
        {
            ApplicationInteractionMode intaractionMode = ApplicationInteractionMode.Touch;
            if (_appearanceSettings.OverrideInteractionMode.HasValue)
            {
                intaractionMode = _appearanceSettings.OverrideInteractionMode.Value;
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
