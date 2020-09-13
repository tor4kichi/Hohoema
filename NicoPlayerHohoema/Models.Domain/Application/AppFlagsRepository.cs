using Microsoft.Toolkit.Uwp.Helpers;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Application
{
    public sealed class AppFlagsRepository : FlagsRepositoryBase
    {
        public bool IsRankingInitialUpdate
        {
            get => Read<bool>();
            set => Save(value);
        }

        public bool IsMigratedCacheFolder_V_0_21_0
        {
            get => Read<bool>();
            set => Save(value);
        }


        public bool IsMigratedCommentFilterSettings_V_0_21_5
        {
            get => Read<bool>();
            set => Save(value);
        }

        public bool IsInitializedCommentFilteringCondition
        {
            get => Read<bool>();
            set => Save(value);
        }

        public bool IsMigratedSubscriptions_V_0_22_0
        {
            get => Read<bool>();
            set => Save(value);
        }

        public bool IsNGScoreZeroFixtureProcessed_V_0_22_1
        {
            get => Read<bool>();
            set => Save(value);
        }

        public bool IsSettingMigrated_V_0_23_0
        {
            get => Read<bool>();
            set => Save(value);
        }

        public bool IsDatabaseMigration_V_0_25_0
        {
            get => Read<bool>();
            set => Save(value);
        }

    }
}
