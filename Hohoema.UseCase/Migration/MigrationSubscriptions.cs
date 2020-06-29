using Hohoema.Models.Repository.App;
using Hohoema.Models.Repository.Subscriptions;
using Hohoema.Models.Subscriptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Migration
{
    public class MigrationSubscriptions
    {
        private readonly AppFlagsRepository _appFlagsRepository;
        private readonly SubscriptionManager _newSubscriptionManager;
        
        public MigrationSubscriptions(
            AppFlagsRepository appFlagsRepository,
            Models.Subscriptions.SubscriptionManager newSubscriptionManager,
            SubscriptionRegistrationRepository subscriptionRegistrationRepository,
            SubscriptionFeedResultRepository subscriptionFeedResultRepository
            )
        {
            _appFlagsRepository = appFlagsRepository;
            _newSubscriptionManager = newSubscriptionManager;

        }

        public void Migration()
        {
#if false
            subscriptionRegistrationRepository.ClearAll();
            subscriptionFeedResultRepository.ClearAll();

            var sources = _newSubscriptionManager.GetAllSubscriptionSourceEntities();
            foreach (var source in sources)
            {
                _newSubscriptionManager.RemoveSubscription(source);
            }

#endif
            if (_appFlagsRepository.IsMigratedSubscriptions_V_0_22_0) { return; }

            Debug.WriteLine($"[MigraionSubscription] Migration start.");

            _appFlagsRepository.IsMigratedSubscriptions_V_0_22_0 = true;

            try
            {
                var allOldSources = Database.Local.Subscription.SubscriptionDb.GetOrderedSubscriptions().SelectMany(x => x.Sources);
                foreach (var oldSource in allOldSources)
                {
                    var type = oldSource.SourceType switch
                    {
                        Models.Subscription.SubscriptionSourceType.User => Models.Subscriptions.SubscriptionSourceType.User,
                        Models.Subscription.SubscriptionSourceType.Channel => Models.Subscriptions.SubscriptionSourceType.Channel,
                        Models.Subscription.SubscriptionSourceType.Mylist => Models.Subscriptions.SubscriptionSourceType.Mylist,
                        Models.Subscription.SubscriptionSourceType.TagSearch => Models.Subscriptions.SubscriptionSourceType.SearchWithKeyword,
                        Models.Subscription.SubscriptionSourceType.KeywordSearch => Models.Subscriptions.SubscriptionSourceType.SearchWithKeyword,
                        _ => throw new NotSupportedException(),
                    };

                    _newSubscriptionManager.AddSubscription(type, oldSource.Parameter, oldSource.Label);

                    Debug.WriteLine($"[MigraionSubscription] <{oldSource.Label}> is done migrate.");
                }
            }
            catch
            {

            }

            Debug.WriteLine($"[MigraionSubscription] All migrate completed.");
        }        
    }
}
