using NicoPlayerHohoema.Helpers;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Subscriptions
{
    public sealed class OpenSubscriptionSourceCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Models.Subscription.SubscriptionSource;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Models.Subscription.SubscriptionSource source)
            {
                var pageManager = Commands.HohoemaCommnadHelper.GetPageManager();

                switch (source.SourceType)
                {
                    case Models.Subscription.SubscriptionSourceType.User:
                        pageManager.OpenPage(Models.HohoemaPageType.UserVideo, source.Parameter);
                        break;
                    case Models.Subscription.SubscriptionSourceType.Channel:
                        pageManager.OpenPage(Models.HohoemaPageType.ChannelVideo, source.Parameter);
                        break;
                    case Models.Subscription.SubscriptionSourceType.Mylist:
                        var mylistPagePayload = new Models.MylistPagePayload(source.Parameter)
                        {
                            Origin = Models.PlaylistOrigin.OtherUser
                        };
                        pageManager.OpenPage(Models.HohoemaPageType.Mylist, mylistPagePayload.ToParameterString());
                        break;
                    case Models.Subscription.SubscriptionSourceType.TagSearch:
                        pageManager.SearchTag(source.Parameter, Mntone.Nico2.Order.Descending, Mntone.Nico2.Sort.FirstRetrieve);
                        break;
                    case Models.Subscription.SubscriptionSourceType.KeywordSearch:
                        pageManager.SearchKeyword(source.Parameter, Mntone.Nico2.Order.Descending, Mntone.Nico2.Sort.FirstRetrieve);
                        break;
                    default:
                        break;
                }


            }
        }
    }
}
