using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Practices.Unity;
using Prism.Windows.Navigation;
using Prism.Windows.AppModel;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
    public sealed partial class PlayerWithPageContainer : ContentControl
    {

        public Frame Frame { get; private set; }

        public PlayerWithPageContainer()
        {
            this.InitializeComponent();
        }

        protected override void OnApplyTemplate()
        {
            Frame = GetTemplateChild("PlayerFrame") as Frame;

            {
                var frameFacade = new FrameFacadeAdapter(Frame);

                var sessionStateService = new SessionStateService();

                var ns = new FrameNavigationService(frameFacade
                    , (pageToken) =>
                    {
                        if (pageToken == nameof(Views.VideoPlayerPage))
                        {
                            return typeof(Views.VideoPlayerPage);
                        }
                        else if (pageToken == nameof(Views.LivePlayerPage))
                        {
                            return typeof(Views.LivePlayerPage);
                        }
                        else
                        {
                            return typeof(Views.BlankPage);
                        }
                    }, sessionStateService);

                (DataContext as ViewModels.PlayerWithPageContainerViewModel).SetNavigationService(ns);
            }

            base.OnApplyTemplate();
        }
    }
    

}
