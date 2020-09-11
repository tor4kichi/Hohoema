using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Legacy;
using Hohoema.Models.UseCase.NicoVideoPlayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Migration
{
    public sealed class MigrationCommentFilteringSettings
    {
        private readonly AppFlagsRepository _appFlagsRepository;
        private readonly PlayerSettings _playerSettings;
        private readonly CommentFiltering _commentFiltering;

        public MigrationCommentFilteringSettings(
            AppFlagsRepository appFlagsRepository,
            PlayerSettings playerSettings,
            CommentFiltering commentFiltering
            )
        {
            _appFlagsRepository = appFlagsRepository;
            _playerSettings = playerSettings;
            _commentFiltering = commentFiltering;
        }


        public void Migration()
        {
            if (!_appFlagsRepository.IsMigratedCommentFilterSettings_V_0_21_5)
            {
                //_playerSettings.NGCommentScore;
                //_playerSettings.CommentGlassMowerEnable;
                //_playerSettings.FilteringCommands;
                //_playerSettings.NGCommentKeywords;

                // NG User Id
                _commentFiltering.IsEnableFilteringCommentOwnerId = _playerSettings.NGCommentUserIdEnable;
                foreach (var ngCommentUserId in _playerSettings.NGCommentUserIds)
                {
                    _commentFiltering.AddFilteringCommentOwnerId(ngCommentUserId.UserId, string.Empty);
                }

                // NG Comment Text
                _commentFiltering.IsEnableFilteringCommentText = _playerSettings.NGCommentKeywordEnable;
                foreach (var ngText in _playerSettings.NGCommentKeywords)
                {
                    _commentFiltering.AddFilteringCommentTextKeyword(ngText.Keyword);
                }

                // Glass Mower と centerコマンドのNG は
                // 移行としてではなく初期化として追加してます

                _appFlagsRepository.IsMigratedCommentFilterSettings_V_0_21_5 = true;
            }
        }
    }
}
