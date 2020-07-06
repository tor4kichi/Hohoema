using Hohoema.Database.Local;
using Hohoema.Models;
using Hohoema.Models.Repository.App;
using Hohoema.Models.Repository.NicoRepo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Attributes;
using Windows.Storage;

namespace Hohoema.UseCase.Migration
{
    public sealed class v0_22_x_MigrationSettings
    {
        [Dependency]
        public AppFlagsRepository _appFlagsRepository { get; set; }

        [Dependency]
        public AppearanceSettingsRepository _appearanceSettingsRepository { get; set; }

        [Dependency]
        public CommentFliteringRepository _commentFliteringRepository{ get; set; }

        [Dependency]
        public PinRepository _pinRepository { get; set; }

        [Dependency]
        public VideoListFilterSettings _videoListFilterSettings { get; set; }

        [Dependency]
        public NicoRepoSettingsRepository _nicoRepoSettingsRepository { get; set; }


        [Dependency]
        public PlayerSettingsRepository _playerSettingsRepository { get; set; }

        [Dependency]
        public RankingSettingsRepository _rankingSettingsRepository { get; set; }

        [Dependency]
        public CacheSettingsRepository _cacheSettingsRepository{ get; set; }


        public async ValueTask Migration()
        {
            if (_appFlagsRepository.V1_0_0_UserSettingsUpgraded)
            {
                return;
            }

            _appFlagsRepository.V1_0_0_UserSettingsUpgraded = true;

#pragma warning disable CS0612 // 型またはメンバーが旧型式です
            var settings = await Models.HohoemaUserSettings.LoadSettings(ApplicationData.Current.LocalFolder);
#pragma warning restore CS0612 // 型またはメンバーが旧型式です

            MigrateAppearanceSettings(settings.AppearanceSettings, _appearanceSettingsRepository);

            MigratePlayerSettings(settings.PlayerSettings, _playerSettingsRepository);

            //MigrateCommentFilteringSettings(settings.PlayerSettings, _commentFliteringRepository);

            MigrateRankingSettings(settings.RankingSettings, _rankingSettingsRepository);

            MigratePinSettings(settings.PinSettings, _pinRepository);

            MigrateNgSettings(settings.NGSettings, _videoListFilterSettings);

            MigrateNicorepoSettings(settings.ActivityFeedSettings, _nicoRepoSettingsRepository);

            MigrateCacheSettings(settings.CacheSettings, _cacheSettingsRepository);
        }

#pragma warning disable CS0612 // 型またはメンバーが旧型式です
        private static void MigrateAppearanceSettings(AppearanceSettings legacy, AppearanceSettingsRepository next)
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
        {
            next.Locale = legacy.Locale;
            next.OverrideIntractionMode = legacy.OverrideIntractionMode == null 
                ? default(Models.Pages.ApplicationInteractionMode?)
                : (Models.Pages.ApplicationInteractionMode)(int)legacy.OverrideIntractionMode.Value
                ;
            next.StartupPageType = legacy.StartupPageType;
            next.AppTheme = legacy.Theme;
        }


#pragma warning disable CS0612 // 型またはメンバーが旧型式です
        private static void MigrateCommentFilteringSettings(PlayerSettings legacy, CommentFliteringRepository next)
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
        {
//            next.ShareNGScore = legacy.NGCommentScore;

            next.IsFilteringCommentTextEnabled = legacy.NGCommentKeywordEnable;
            foreach (var ngCommentKeyword in legacy.NGCommentKeywords ?? Enumerable.Empty<Models.NGKeyword>())
            {
                next.AddFilteringCommentText(ngCommentKeyword.Keyword);
            }

            next.IsFilteringCommentOwnerIdEnabled = legacy.NGCommentUserIdEnable;
            foreach (var ngOwner in legacy.NGCommentUserIds ?? Enumerable.Empty<Models.UserIdInfo>())
            {
                next.AddFilteringCommenOwnerId(ngOwner.UserId, ngOwner.Description);
            }


        }

#pragma warning disable CS0612 // 型またはメンバーが旧型式です
        private static void MigratePinSettings(PinSettings legacy, PinRepository next)
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
        {
#pragma warning disable CS0612 // 型またはメンバーが旧型式です
            foreach (var legacyPin in legacy.Pins ?? Enumerable.Empty<HohoemaPin>())
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
            {
                next.CreateItem(new Models.Pages.HohoemaPin() 
                {
                    Label = legacyPin.Label,
                    OverrideLabel = legacyPin.OverrideLabel,
                    PageType = legacyPin.PageType,
                    Parameter = legacyPin.Parameter
                });
            }
        }

#pragma warning disable CS0612 // 型またはメンバーが旧型式です
        private static void MigrateNgSettings(NGSettings legacy, VideoListFilterSettings next)
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
        {
            next.NGVideoIdEnable = legacy.NGVideoIdEnable;
            next.NGVideoOwnerUserIdEnable = legacy.NGVideoOwnerUserIdEnable;
            next.NGVideoTitleKeywordEnable = legacy.NGVideoTitleKeywordEnable;

            foreach (var ngVideoId in legacy.NGVideoIds ?? Enumerable.Empty<Models.VideoIdInfo>())
            {
                next.AddNGVideo(ngVideoId.VideoId, ngVideoId.Description);
            }

            foreach (var ngOwner in legacy.NGVideoOwnerUserIds ?? Enumerable.Empty<Models.UserIdInfo>())
            {
                next.AddNgVideoOwner(ngOwner.UserId, ngOwner.Description);
            }

            foreach (var ngTitle in legacy.NGVideoTitleKeywords ?? Enumerable.Empty<Models.NGKeyword>())
            {
                next.AddNGVideoTitleKeyword(ngTitle.Keyword, ngTitle.TestText);
            }
        }

#pragma warning disable CS0612 // 型またはメンバーが旧型式です
        private static void MigrateNicorepoSettings(ActivityFeedSettings legacy, NicoRepoSettingsRepository next)
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
        {
            next.DisplayNicoRepoItemTopics = legacy.DisplayNicoRepoItemTopics.ToArray();
        }

#pragma warning disable CS0612 // 型またはメンバーが旧型式です
        private static void MigratePlayerSettings(PlayerSettings legacy, PlayerSettingsRepository next)
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
        {
            next.PlaybackRate = legacy.PlaybackRate;
            next.PlaylistEndAction = (Models.Repository.Playlist.PlaylistEndAction)(int)legacy.PlaylistEndAction;
            next.PauseWithCommentWriting = legacy.PauseWithCommentWriting;
            next.NicoScript_Default_Enabled= legacy.NicoScript_Default_Enabled;
            next.NicoScript_DisallowComment_Enabled = legacy.NicoScript_DisallowComment_Enabled;
            next.NicoScript_DisallowSeek_Enabled = legacy.NicoScript_DisallowSeek_Enabled;
            next.NicoScript_Jump_Enabled = legacy.NicoScript_Jump_Enabled;
            next.NicoScript_Replace_Enabled = legacy.NicoScript_Replace_Enabled;

            next.IsShuffleEnable = legacy.IsShuffleEnable;
            next.IsReverseModeEnable = legacy.IsReverseModeEnable;
            next.IsLoudnessCorrectionEnabled = legacy.IsLoudnessCorrectionEnabled;
            next.IsCommentDisplay_Video = legacy.IsCommentDisplay_Video;
            next.DefaultQuality = legacy.DefaultQuality;
            next.DefaultCommentFontScale = legacy.DefaultCommentFontScale;
            next.CommentDisplayDuration = legacy.CommentDisplayDuration;
            next.CommentColor = legacy.CommentColor;
            next.CommentOpacity = legacy.CommentOpacity;
            next.AutoMoveNextVideoOnPlaylistEmpty = legacy.AutoMoveNextVideoOnPlaylistEmpty;
            next.AutoHidePlayerControlUIPreventTime = legacy.AutoHidePlayerControlUIPreventTime;
            next.IsDefaultCommentWithAnonymous = legacy.IsDefaultCommentWithAnonymous;
            next.IsCurrentVideoLoopingEnabled = legacy.IsCurrentVideoLoopingEnabled;
            next.IsMute = legacy.IsMute;
            next.SoundVolume = legacy.SoundVolume;
            next.SoundVolumeChangeFrequency = legacy.SoundVolumeChangeFrequency;
            next.IsForceLandscape = legacy.IsForceLandscape;
        }

#pragma warning disable CS0612 // 型またはメンバーが旧型式です
        private static void MigrateCacheSettings(CacheSettings legacy, CacheSettingsRepository next)
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
        {
            next.IsCacheEnabled = legacy.IsEnableCache;
            next.IsCacheAccepted = legacy.IsUserAcceptedCache;
            next.DefaultCacheQuality = legacy.DefaultCacheQuality;
        }


#pragma warning disable CS0612 // 型またはメンバーが旧型式です
        private static void MigrateRankingSettings(RankingSettings legacy, RankingSettingsRepository next)
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
        {
            Models.Repository.Niconico.NicoVideo.Ranking.RankingGenreTag ToNextrankingGenreTag(Models.RankingGenreTag rankingGenreTag)
            {
                return new Models.Repository.Niconico.NicoVideo.Ranking.RankingGenreTag()
                {
                    Genre = rankingGenreTag.Genre,
                    Label = rankingGenreTag.Label,
                    Tag = rankingGenreTag.Tag,
                };
            }

            next.FavoriteTags = legacy.FavoriteTags.Select(ToNextrankingGenreTag).ToArray();
            next.HiddenGenres = legacy.HiddenGenres.ToArray();
            next.HiddenTags = legacy.HiddenTags.Select(ToNextrankingGenreTag).ToArray();            
        }
    }
}
