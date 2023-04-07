using Hohoema.Models.Application;
using Hohoema.Models.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services.Migration
{
    public sealed class SubscriptionMigration_1_3_13 : IMigrateSync
    {
        private readonly SubscFeedVideoRepository _subscFeedVideoRepository;
        private readonly AppFlagsRepository _appFlagsRepository;
        private readonly SubscriptionRegistrationRepository _subscriptionRegistrationRepository;
        private readonly SubscriptionFeedResultRepository _subscriptionFeedResultRepository;

        public SubscriptionMigration_1_3_13(
            AppFlagsRepository appFlagsRepository,
            SubscriptionRegistrationRepository subscriptionRegistrationRepository,
            SubscriptionFeedResultRepository subscriptionFeedResultRepository,
            SubscFeedVideoRepository subscFeedVideoRepository
            )
        {
            _subscFeedVideoRepository = subscFeedVideoRepository;
            _appFlagsRepository = appFlagsRepository;
            _subscriptionRegistrationRepository = subscriptionRegistrationRepository;
            _subscriptionFeedResultRepository = subscriptionFeedResultRepository;
        }        

        public void Migrate()
        {
            if (_appFlagsRepository.IsSubscriptionMigrate_1_3_13) { return; }

            var subscItems = _subscriptionRegistrationRepository.ReadAllItems();

            DateTime now = DateTime.Now;
            foreach (var subsc in subscItems)
            {
                var result = _subscriptionFeedResultRepository.GetFeedResult(subsc);

                _subscFeedVideoRepository.RegisteringVideosIfNotExist(subsc.Id, now, result.Videos.Select(x => new SubscFeedVideo 
                {
                    PostAt = x.PostAt,
                    SourceSubscId = subsc.Id,
                    Title = x.Title,
                    VideoId = x.VideoId,     
                }));
            }

            _appFlagsRepository.IsSubscriptionMigrate_1_3_13 = true;
        }
    }
}
