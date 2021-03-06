﻿using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Subscriptions
{
    public sealed class SubscriptionSettings : FlagsRepositoryBase
    { 
        public bool IsSubscriptionAutoUpdateEnabled
        {
            get => Read(true);
            set => Save(value);
        }

        public DateTime SubscriptionsLastUpdatedAt
        {
            get => Read(DateTime.Now);
            set => Save(value);
        }

        public TimeSpan SubscriptionsUpdateFrequency
        {
            get => Read(TimeSpan.FromMinutes(60));
            set => Save(value);
        }



        public bool IsSortWithSubscriptionUpdated
        {
            get => Read(true);
            set => Save(value);
        }
    }
}
