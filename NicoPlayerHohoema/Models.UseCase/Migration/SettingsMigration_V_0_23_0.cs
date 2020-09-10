using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Legacy;
using Hohoema.Models.Domain.Niconico.Video;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Models.UseCase.Migration
{
    public sealed class SettingsMigration_V_0_23_0 : IMigrateAsync
    {
        private readonly AppFlagsRepository _appFlagsRepository;
        private readonly Domain.Application.AppearanceSettings _appearanceSettings;
        private readonly Domain.Application.PinSettings _pinSettings;
        private readonly VideoRankingSettings _videoRankingSettings;
        private readonly VideoFilteringSettings _videoFilteringRepository;

        public SettingsMigration_V_0_23_0(
            AppFlagsRepository appFlagsRepository,
            Domain.Application.AppearanceSettings appearanceSettings,
            Domain.Application.PinSettings pinSettings,
            VideoRankingSettings videoRankingSettings,
            VideoFilteringSettings videoFilteringRepository
            )
        {
            _appFlagsRepository = appFlagsRepository;
            _appearanceSettings = appearanceSettings;
            _pinSettings = pinSettings;
            _videoRankingSettings = videoRankingSettings;
            _videoFilteringRepository = videoFilteringRepository;
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
                        _pinSettings.CreateItem(new Domain.PageNavigation.HohoemaPin()
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
