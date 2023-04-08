using Hohoema.Models;
using Hohoema.Models.Application;
using Hohoema.Services.Player.Videos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services.Migrations
{
    public sealed class CommentFilteringNGScoreZeroFixture
    {
        private readonly AppFlagsRepository _appFlagsRepository;
        private readonly CommentFilteringFacade _commentFiltering;

        public CommentFilteringNGScoreZeroFixture(
            CommentFilteringFacade commentFiltering,
            AppFlagsRepository appFlagsRepository
            )
        {
            _appFlagsRepository = appFlagsRepository;
            _commentFiltering = commentFiltering;
        }

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
}
