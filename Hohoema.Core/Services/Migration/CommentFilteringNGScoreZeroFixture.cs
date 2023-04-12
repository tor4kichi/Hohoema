#nullable enable
using Hohoema.Models.Application;
using Hohoema.Services.Player.Videos;

namespace Hohoema.Services.Migrations;

public sealed class CommentFilteringNGScoreZeroFixture
{
    [System.Obsolete]
    private readonly AppFlagsRepository _appFlagsRepository;
    private readonly CommentFilteringFacade _commentFiltering;

    [System.Obsolete]
    public CommentFilteringNGScoreZeroFixture(
        CommentFilteringFacade commentFiltering,
        AppFlagsRepository appFlagsRepository
        )
    {
        _appFlagsRepository = appFlagsRepository;
        _commentFiltering = commentFiltering;
    }

    [System.Obsolete]
    public void Migration()
    {
        if (!_appFlagsRepository.IsNGScoreZeroFixtureProcessed_V_0_22_1)
        {
            _appFlagsRepository.IsNGScoreZeroFixtureProcessed_V_0_22_1 = true;
            if (_commentFiltering.ShareNGScore == 0)
            {
                _commentFiltering.ShareNGScore = -10000;
            }
        }
    }
}
