using Hohoema.Models;
using Hohoema.Models.Application;
using Hohoema.Models.Legacy;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Player;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using PlayerSettings = Hohoema.Models.Player.PlayerSettings;

namespace Hohoema.Models.UseCase.Migration
{
    public sealed class SettingsMigration_V_0_23_0 : IMigrateAsync
    {
        private readonly AppFlagsRepository _appFlagsRepository;
        private readonly Application.AppearanceSettings _appearanceSettings;
        private readonly Pins.PinSettings _pinSettings;
        private readonly VideoRankingSettings _videoRankingSettings;
        private readonly VideoFilteringSettings _videoFilteringRepository;
        private readonly PlayerSettings _playerSettings;

        public SettingsMigration_V_0_23_0(
            AppFlagsRepository appFlagsRepository,
            Application.AppearanceSettings appearanceSettings,
            Pins.PinSettings pinSettings,
            VideoRankingSettings videoRankingSettings,
            VideoFilteringSettings videoFilteringRepository,
            PlayerSettings playerSettings
            )
        {
            _appFlagsRepository = appFlagsRepository;
            _appearanceSettings = appearanceSettings;
            _pinSettings = pinSettings;
            _videoRankingSettings = videoRankingSettings;
            _videoFilteringRepository = videoFilteringRepository;
            _playerSettings = playerSettings;
        }

        public async Task MigrateAsync()
        {
            if (_appFlagsRepository.IsSettingMigrated_V_0_23_0) { return; }

            try
            {
                var hohoemaUserSettings = await HohoemaUserSettings.LoadSettings(ApplicationData.Current.LocalFolder);


                Debug.WriteLine("[Migrate] AppearanceSetting");
                {
                    var appearanceSettings = hohoemaUserSettings.AppearanceSettings;
                    _appearanceSettings.Locale = appearanceSettings.Locale;
                    _appearanceSettings.FirstAppearPageType = appearanceSettings.FirstAppearPageType;
                    _appearanceSettings.OverrideInteractionMode = appearanceSettings.OverrideIntractionMode;
                    _appearanceSettings.ApplicationTheme = appearanceSettings.Theme;
                }
                Debug.WriteLine("[Migrate] AppearanceSetting done");

                Debug.WriteLine("[Migrate] RankingSetting");
                {
                    var rankingSettings = hohoemaUserSettings.RankingSettings;
                    foreach (var favRankingTag in rankingSettings.FavoriteTags)
                    {
                        _videoRankingSettings.AddFavoriteTag(favRankingTag.Genre, favRankingTag.Tag, favRankingTag.Label);
                    }

                    Debug.WriteLine("[Migrate] RankingSetting FavoriteTag migrated. ");

                    foreach (var hiddenRankingTag in rankingSettings.HiddenTags)
                    {
                        _videoRankingSettings.AddHiddenTag(hiddenRankingTag.Genre, hiddenRankingTag.Tag, hiddenRankingTag.Label);
                    }

                    Debug.WriteLine("[Migrate] RankingSetting HiddenTags migrated. ");

                    foreach (var hiddenGenre in rankingSettings.HiddenGenres)
                    {
                        _videoRankingSettings.AddHiddenGenre(hiddenGenre);
                    }

                    Debug.WriteLine("[Migrate] RankingSetting HiddenGenres migrated. ");
                }
                Debug.WriteLine("[Migrate] RankingSetting done");

                Debug.WriteLine("[Migrate] PinSetting");
                {
                    var pinSettings = hohoemaUserSettings.PinSettings;

                    int index = 0;
                    foreach (var pin in pinSettings.Pins)
                    {
                        _pinSettings.CreateItem(new Pins.HohoemaPin()
                        {
                            Label = pin.Label,
                            Parameter = pin.Parameter,
                            OverrideLabel = pin.OverrideLabel,
                            PageType = pin.PageType,
                            SortIndex = index
                        });

                        index++;
                    }
                    
                }

                Debug.WriteLine("[Migrate] PinSetting done");

                Debug.WriteLine("[Migrate] NGSetting(VideoFilteringSettings)");
                {
                    var ngSettings = hohoemaUserSettings.NGSettings;
                    _videoFilteringRepository.NGVideoIdEnable = ngSettings.NGVideoIdEnable;
                    _videoFilteringRepository.NGVideoOwnerUserIdEnable = ngSettings.NGVideoOwnerUserIdEnable;
                    _videoFilteringRepository.NGVideoTitleKeywordEnable = ngSettings.NGVideoTitleKeywordEnable;

                    foreach (var videoId in  ngSettings.NGVideoIds)
                    {
                        _videoFilteringRepository.AddHiddenVideoId(videoId.VideoId, videoId.Description);
                    }

                    Debug.WriteLine("[Migrate] NGSetting ng video ids migrated");

                    foreach (var userId in ngSettings.NGVideoOwnerUserIds)
                    {
                        _videoFilteringRepository.AddHiddenVideoOwnerId(userId.UserId, userId.Description);
                    }

                    Debug.WriteLine("[Migrate] NGSetting ng video owner ids migrated");

                    foreach (var ngTitle in ngSettings.NGVideoTitleKeywords)
                    {
                        var titleEntry = _videoFilteringRepository.CreateVideoTitleFiltering();
                        titleEntry.Keyword = ngTitle.Keyword;
                        _videoFilteringRepository.UpdateVideoTitleFiltering(titleEntry);
                    }

                    Debug.WriteLine("[Migrate] NGSetting ng video title migrated");
                }
                Debug.WriteLine("[Migrate] NGSetting(VideoFilteringSettings) done");


                {
                    var ps= hohoemaUserSettings.PlayerSettings;
                    _playerSettings.DefaultVideoQuality = ps.DefaultVideoQuality;
                    _playerSettings.DefaultLiveQuality = ps.DefaultLiveQuality;
                    _playerSettings.LiveWatchWithLowLatency = ps.LiveWatchWithLowLatency;
                    _playerSettings.IsMute = ps.IsMute;
                    _playerSettings.SoundVolume = ps.SoundVolume;
                    _playerSettings.SoundVolumeChangeFrequency = ps.SoundVolumeChangeFrequency;
                    _playerSettings.IsLoudnessCorrectionEnabled = ps.IsLoudnessCorrectionEnabled;
                    _playerSettings.IsCommentDisplay_Video = ps.IsCommentDisplay_Video;
                    _playerSettings.IsCommentDisplay_Live = ps.IsCommentDisplay_Live;
                    _playerSettings.PauseWithCommentWriting = ps.PauseWithCommentWriting;
                    _playerSettings.CommentDisplayDuration = ps.CommentDisplayDuration;
                    _playerSettings.DefaultCommentFontScale = ps.DefaultCommentFontScale;
                    _playerSettings.CommentOpacity = ps.CommentOpacity;
                    _playerSettings.IsDefaultCommentWithAnonymous = ps.IsDefaultCommentWithAnonymous;
                    _playerSettings.CommentColor = ps.CommentColor;
                    _playerSettings.IsAutoHidePlayerControlUI = ps.IsAutoHidePlayerControlUI;
                    _playerSettings.AutoHidePlayerControlUIPreventTime = ps.AutoHidePlayerControlUIPreventTime;
                    _playerSettings.IsForceLandscape = ps.IsForceLandscape;
                    _playerSettings.PlaybackRate = ps.PlaybackRate;
                    _playerSettings.NicoScript_Default_Enabled = ps.NicoScript_Default_Enabled;
                    _playerSettings.NicoScript_DisallowSeek_Enabled = ps.NicoScript_DisallowSeek_Enabled;
                    _playerSettings.NicoScript_Jump_Enabled = ps.NicoScript_Jump_Enabled;
                    _playerSettings.NicoScript_DisallowComment_Enabled = ps._NicoScript_DisallowComment_Enabled;
                    _playerSettings.NicoScript_Replace_Enabled = ps.NicoScript_Replace_Enabled;
                    _playerSettings.IsCurrentVideoLoopingEnabled = ps.IsCurrentVideoLoopingEnabled;
                    _playerSettings.IsPlaylistLoopingEnabled = ps.IsPlaylistLoopingEnabled;
                    _playerSettings.IsShuffleEnable = ps.IsShuffleEnable;
                    _playerSettings.IsReverseModeEnable = ps.IsReverseModeEnable;
                    _playerSettings.PlaylistEndAction = ps.PlaylistEndAction switch
                    {
                        Legacy.PlaylistEndAction.NothingDo => Player.PlaylistEndAction.NothingDo,
                        Legacy.PlaylistEndAction.ChangeIntoSplit => Player.PlaylistEndAction.ChangeIntoSplit,
                        Legacy.PlaylistEndAction.CloseIfPlayWithCurrentWindow => Player.PlaylistEndAction.CloseIfPlayWithCurrentWindow,
                        _ => throw new NotSupportedException()
                    };
                    _playerSettings.AutoMoveNextVideoOnPlaylistEmpty = ps.AutoMoveNextVideoOnPlaylistEmpty;
                    
                }


                {
                    var allSettings = new SettingsBase[]
                    {
                        hohoemaUserSettings.RankingSettings,
                        hohoemaUserSettings.PlayerSettings,
                        hohoemaUserSettings.PinSettings,
                        hohoemaUserSettings.NGSettings,
                        hohoemaUserSettings.CacheSettings,
                        hohoemaUserSettings.AppearanceSettings,
                        hohoemaUserSettings.ActivityFeedSettings
                    };

                    hohoemaUserSettings.Dispose();

                    foreach (var setting in allSettings)
                    {
                        Debug.WriteLine("[Migrate] Delete legacy settings :" + setting.FileName);
                        var file = await setting.GetFile();
                        
                        await file.DeleteAsync();
                    }
                }
            }
            catch
            {

            }
            finally
            {
                _appFlagsRepository.IsSettingMigrated_V_0_23_0 = true;
            }
        }
    }
}
