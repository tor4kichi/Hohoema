using Hohoema.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Subscriptions
{
    public sealed class SubscriptionSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch (value)
            {
                case Interfaces.IVideoContent video:

                    var ownerInfo = Database.NicoVideoOwnerDb.Get(video.ProviderId);
                    if (ownerInfo != null)
                    {
                        if (ownerInfo.UserType == NicoVideoUserType.User)
                        {
                            return new Models.Subscription.SubscriptionSource(
                                ownerInfo.ScreenName,
                                Models.Subscription.SubscriptionSourceType.User,
                                ownerInfo.OwnerId
                                );
                        }
                        else if (ownerInfo.UserType == NicoVideoUserType.Channel)
                        {
                            return new Models.Subscription.SubscriptionSource(
                                ownerInfo.ScreenName,
                                Models.Subscription.SubscriptionSourceType.Channel,
                                ownerInfo.OwnerId
                                );
                        }
                    }
                    break;
                case Interfaces.IUser user:
                    return new Models.Subscription.SubscriptionSource(
                        user.Label,
                        Models.Subscription.SubscriptionSourceType.User,
                        user.Id
                        );
                case Interfaces.IChannel channel:
                    return new Models.Subscription.SubscriptionSource(
                        channel.Label,
                        Models.Subscription.SubscriptionSourceType.Channel,
                        channel.Id
                        );
                case Interfaces.IMylist mylist:
                    return new Models.Subscription.SubscriptionSource(
                        mylist.Label,
                        Models.Subscription.SubscriptionSourceType.Mylist,
                        mylist.Id
                        );
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
