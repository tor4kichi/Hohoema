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


        public bool IsSearchQueryInPinsMigration_V_0_26_0
        {
            get => Read<bool>();
            set => Save(value);
        }

        public bool IsLocalMylistThumbnailImageMigration_V_0_28_0
        {
            get => Read<bool>();
            set => Save(value);
        }



        public bool NowCacheVideosMigrating_V_0_29_0
        {
            get => Read<bool>();
            internal set => Save(value);
        }


        public bool IsCacheVideosMigrated_V_0_29_0
        {
            get => Read<bool>();
            internal set => Save(value);
        }


        internal CacheVideoMigrationScope GetCacheVideoMigration()
        {
            if (IsCacheVideosMigrated_V_0_29_0 == true)
            {
                throw new InvalidOperationException();
            }

            if (NowCacheVideosMigrating_V_0_29_0 == true)
            {
                throw new InvalidOperationException();
            }

            return new CacheVideoMigrationScope(this);
        }

        internal class CacheVideoMigrationScope : IDisposable
        {
            private readonly AppFlagsRepository _appFlagsRepository;

            public CacheVideoMigrationScope(AppFlagsRepository appFlagsRepository)
            {
                _appFlagsRepository = appFlagsRepository;
                _appFlagsRepository.NowCacheVideosMigrating_V_0_29_0 = true;
            }

            public void Complete()
            {
                _appFlagsRepository.IsCacheVideosMigrated_V_0_29_0 = true;
            }

            public void Dispose()
            {
                _appFlagsRepository.NowCacheVideosMigrating_V_0_29_0 = false;
            }
        }


    }
}
