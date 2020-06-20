using NicoPlayerHohoema.Models.Subscriptions;
using NicoPlayerHohoema.Repository;
using NicoPlayerHohoema.Repository.Subscriptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Migration
{
    public class MigrationSubscriptions
    {
        private readonly AppFlagsRepository _appFlagsRepository;
        private readonly Models.Subscriptions.SubscriptionManager _newSubscriptionManager;
        private readonly Models.Subscription.SubscriptionManager _oldSubscriptionManager;

        public MigrationSubscriptions(
            AppFlagsRepository appFlagsRepository,
            Models.Subscriptions.SubscriptionManager newSubscriptionManager,
            Models.Subscription.SubscriptionManager oldSubscriptionManager,
            SubscriptionRegistrationRepository subscriptionRegistrationRepository,
            SubscriptionFeedResultRepository subscriptionFeedResultRepository
            )
        {
            _appFlagsRepository = appFlagsRepository;
            _newSubscriptionManager = newSubscriptionManager;
            _oldSubscriptionManager = oldSubscriptionManager;

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
                var allOldSources = _oldSubscriptionManager.Subscriptions.SelectMany(x => x.Sources);
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
