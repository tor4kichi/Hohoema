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
using Unity;
using Hohoema.Models.Domain;
using Prism.Ioc;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;

namespace Hohoema.Presentation.Views.Flyouts
{
    public sealed partial class FollowItemFlyout : MenuFlyout
    {
        public FollowItemFlyout()
        {
            this.InitializeComponent();

            _followManager = App.Current.Container.Resolve<FollowManager>();
            FollowRemoveCommand = App.Current.Container.Resolve<FollowRemoveCommand>();

            Opening += FollowItemFlyout_Opening;
        }

        private void FollowItemFlyout_Opening(object sender, object e)
        {
            RemoveFollowButton.Command = FollowRemoveCommand;
        }

        FollowManager _followManager;

        public FollowRemoveCommand FollowRemoveCommand { get; private set; }
    }
}
