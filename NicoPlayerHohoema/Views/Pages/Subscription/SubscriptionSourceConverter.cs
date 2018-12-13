using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Subscriptions
{
    public sealed class SubscriptionSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch (value)
            {
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
                case Interfaces.IVideoContent video:
                    if (string.IsNullOrEmpty(video.OwnerUserId))
                    {
                        break;
                    }
                    Models.Subscription.SubscriptionSourceType? souceType = null;
                    string ownerName = video.OwnerUserName;
                    switch (video.OwnerUserType)
                    {
                        case Mntone.Nico2.Videos.Thumbnail.UserType.User:
                            souceType = Models.Subscription.SubscriptionSourceType.User;
                            break;
                        case Mntone.Nico2.Videos.Thumbnail.UserType.Channel:
                            souceType = Models.Subscription.SubscriptionSourceType.Channel;
                            break;
                        default:
                            break;
                    }

                    if (souceType.HasValue)
                    {
                        return new Models.Subscription.SubscriptionSource(
                            video.OwnerUserName,
                            souceType.Value,
                            video.OwnerUserId
                            );
                    }
                    break;
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
