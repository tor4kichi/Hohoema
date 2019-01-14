using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;
using Prism.Commands;
using Prism.Navigation;

namespace NicoPlayerHohoema.Views.Subscriptions
{
    public sealed class OpenSubscriptionSourceCommand : DelegateCommandBase
    {
        public OpenSubscriptionSourceCommand(Services.PageManager pageManager)
        {
            PageManager = pageManager;
        }

        public Services.PageManager PageManager { get; }

        protected override bool CanExecute(object parameter)
        {
            return parameter is Models.Subscription.SubscriptionSource;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Models.Subscription.SubscriptionSource source)
            {
                switch (source.SourceType)
                {
                    case Models.Subscription.SubscriptionSourceType.User:
                        PageManager.OpenPageWithId(HohoemaPageType.UserVideo, source.Parameter);
                        break;
                    case Models.Subscription.SubscriptionSourceType.Channel:
                        PageManager.OpenPageWithId(HohoemaPageType.ChannelVideo, source.Parameter);
                        break;
                    case Models.Subscription.SubscriptionSourceType.Mylist:
                        var p = new NavigationParameters
                        {
                            { "id", source.Parameter },
                            { "origin", Services.PlaylistOrigin.OtherUser }
                        };
                        PageManager.OpenPage(HohoemaPageType.Mylist, p);
                        break;
                    case Models.Subscription.SubscriptionSourceType.TagSearch:
                        PageManager.Search(Models.SearchTarget.Tag, source.Parameter);
                        break;
                    case Models.Subscription.SubscriptionSourceType.KeywordSearch:
                        PageManager.Search(Models.SearchTarget.Keyword, source.Parameter);
                        break;
                    default:
                        break;
                }


            }
        }
    }
}
