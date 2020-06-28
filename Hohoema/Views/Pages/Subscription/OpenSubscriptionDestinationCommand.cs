using Hohoema.Services;
using Hohoema.Services.Page;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Views.Subscriptions
{
    public sealed class OpenSubscriptionDestinationCommand : DelegateCommandBase
    {
        public OpenSubscriptionDestinationCommand(PageManager pageManager)
        {
            PageManager = pageManager;
        }

        public PageManager PageManager { get; }

        protected override bool CanExecute(object parameter)
        {
            return parameter is Models.Subscription.SubscriptionDestination;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Models.Subscription.SubscriptionDestination dest)
            {
                if (dest.Target == Models.Subscription.SubscriptionDestinationTarget.LoginUserMylist)
                {
                    var mylistPagePayload = new MylistPagePayload(dest.PlaylistId);
                    PageManager.OpenPage(HohoemaPageType.Mylist, mylistPagePayload.ToParameterString());
                }
                else if (dest.Target == Models.Subscription.SubscriptionDestinationTarget.LocalPlaylist)
                {
                    PageManager.OpenPage(HohoemaPageType.LocalPlaylist, "id=" + dest.PlaylistId);
                }
            }
        }
    }
}
