using Hohoema.Contracts.Migrations;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico.Search;

namespace Hohoema.Services.Migrations;

public sealed class SearchTargetMigration_V_1_1_0 : IMigrateSync
{
    private readonly SearchHistoryRepository _searchHistoryRepository;
    [System.Obsolete]
    private readonly AppFlagsRepository _appFlagsRepository;

    [System.Obsolete]
    public SearchTargetMigration_V_1_1_0(
        SearchHistoryRepository searchHistoryRepository,
        AppFlagsRepository appFlagsRepository
        )
    {
        _searchHistoryRepository = searchHistoryRepository;
        _appFlagsRepository = appFlagsRepository;
    }

    [System.Obsolete]
    public void Migrate()
    {
        if (_appFlagsRepository.IsSearchTargetOmmitMylistAndCommunityMigrated_V_1_1_0) { return; }

        _appFlagsRepository.IsSearchTargetOmmitMylistAndCommunityMigrated_V_1_1_0 = true;

        _searchHistoryRepository.Clear();
    }
}
