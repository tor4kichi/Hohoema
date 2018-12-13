using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Subscriptions
{
    public sealed class OpenSubscriptionDestinationCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Models.Subscription.SubscriptionDestination;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Models.Subscription.SubscriptionDestination dest)
            {
                var pageManager = Commands.HohoemaCommnadHelper.GetPageManager();
                var mylistPagePayload = new Models.MylistPagePayload(dest.PlaylistId)
                {
                    Origin = dest.Target == Models.Subscription.SubscriptionDestinationTarget.LocalPlaylist 
                    ? Models.PlaylistOrigin.Local 
                    : Models.PlaylistOrigin.LoginUser
                };
                pageManager.OpenPage(Models.HohoemaPageType.Mylist, mylistPagePayload.ToParameterString());
            }
        }
    }
}
