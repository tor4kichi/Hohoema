using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Repository.Subscriptions
{
    public sealed class SubscriptionSettingsRepository : FlagsRepositoryBase
    {
        public DateTime SubscriptionsLastUpdatedAt
        {
            get => Read<DateTime>(DateTime.Now);
            set => Save(value);
        }

        public TimeSpan SubscriptionsUpdateFrequency
        {
            get => Read<TimeSpan>(TimeSpan.FromMinutes(60));
            set => Save(value);
        }

    }
}
