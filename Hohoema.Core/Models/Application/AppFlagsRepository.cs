using Hohoema.Infra;

namespace Hohoema.Models.Application;

[System.Obsolete]
public sealed class AppFlagsRepository : FlagsRepositoryBase
{
    [System.Obsolete]
    public bool IsRankingInitialUpdate
    {
        get => Read<bool>();
        set => Save(value);
    }

    [System.Obsolete]
    public bool IsMigratedCacheFolder_V_0_21_0
    {
        get => Read<bool>();
        set => Save(value);
    }


    [System.Obsolete]
    public bool IsMigratedCommentFilterSettings_V_0_21_5
    {
        get => Read<bool>();
        set => Save(value);
    }

    [System.Obsolete]
    public bool IsInitializedCommentFilteringCondition
    {
        get => Read<bool>();
        set => Save(value);
    }

    [System.Obsolete]
    public bool IsMigratedSubscriptions_V_0_22_0
    {
        get => Read<bool>();
        set => Save(value);
    }

    [System.Obsolete]
    public bool IsNGScoreZeroFixtureProcessed_V_0_22_1
    {
        get => Read<bool>();
        set => Save(value);
    }

    [System.Obsolete]
    public bool IsSettingMigrated_V_0_23_0
    {
        get => Read<bool>();
        set => Save(value);
    }

    [System.Obsolete]
    public bool IsDatabaseMigration_V_0_25_0
    {
        get => Read<bool>();
        set => Save(value);
    }


    [System.Obsolete]
    public bool IsSearchQueryInPinsMigration_V_0_26_0
    {
        get => Read<bool>();
        set => Save(value);
    }

    [System.Obsolete]
    public bool IsLocalMylistThumbnailImageMigration_V_0_28_0
    {
        get => Read<bool>();
        set => Save(value);
    }



    [System.Obsolete]
    public bool IsCacheVideosMigrated_V_0_29_0
    {
        get => Read<bool>();
        set => Save(value);
    }



    [System.Obsolete]
    public bool IsSearchTargetOmmitMylistAndCommunityMigrated_V_1_1_0
    {
        get => Read<bool>();
        set => Save(value);
    }


    [System.Obsolete]
    public bool IsSubscriptionMigrate_1_3_13
    {
        get => Read<bool>();
        set => Save(value);
    }
}
