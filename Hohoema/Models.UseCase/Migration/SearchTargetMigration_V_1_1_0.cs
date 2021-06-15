using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Niconico.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Migration
{
    internal sealed class SearchTargetMigration_V_1_1_0 : IMigrateSync
    {
        private readonly SearchHistoryRepository _searchHistoryRepository;
        private readonly AppFlagsRepository _appFlagsRepository;

        public SearchTargetMigration_V_1_1_0(
            SearchHistoryRepository searchHistoryRepository,
            AppFlagsRepository appFlagsRepository
            )
        {
            _searchHistoryRepository = searchHistoryRepository;
            _appFlagsRepository = appFlagsRepository;
        }
        public void Migrate()
        {
            if (_appFlagsRepository.IsSearchTargetOmmitMylistAndCommunityMigrated_V_1_1_0) { return; }

            _appFlagsRepository.IsSearchTargetOmmitMylistAndCommunityMigrated_V_1_1_0 = true;

            _searchHistoryRepository.Clear();
        }
    }
}
