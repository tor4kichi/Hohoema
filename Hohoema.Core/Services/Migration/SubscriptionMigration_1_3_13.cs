using Hohoema.Contracts.Migrations;
using Hohoema.Models.Application;
using Hohoema.Models.Subscriptions;
using System;
using System.Linq;

namespace Hohoema.Services.Migrations;

public sealed class SubscriptionMigration_1_3_13 : IMigrateSync
{
    private readonly SubscFeedVideoRepository _subscFeedVideoRepository;
    [Obsolete]
    private readonly AppFlagsRepository _appFlagsRepository;
    private readonly SubscriptionRegistrationRepository _subscriptionRegistrationRepository;
    private readonly SubscriptionFeedResultRepository _subscriptionFeedResultRepository;

    [Obsolete]
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

    [Obsolete]
    public void Migrate()
    {
        if (_appFlagsRepository.IsSubscriptionMigrate_1_3_13) { return; }

        System.Collections.Generic.List<SubscriptionSourceEntity> subscItems = _subscriptionRegistrationRepository.ReadAllItems();

        DateTime now = DateTime.Now;
        foreach (SubscriptionSourceEntity subsc in subscItems)
        {
            SubscriptionFeedResult result = _subscriptionFeedResultRepository.GetFeedResult(subsc);

            _ = _subscFeedVideoRepository.RegisteringVideosIfNotExist(subsc.Id, now, result.Videos.Select(x => new SubscFeedVideo
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
