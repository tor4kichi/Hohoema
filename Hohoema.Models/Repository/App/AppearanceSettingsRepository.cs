using Hohoema.Models.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.App
{
    public sealed class AppearanceSettingsRepository : FlagsRepositoryBase
    {
        public AppearanceSettingsRepository()
        {
            _StartupPageType = Read(HohoemaPageType.RankingCategoryList, nameof(StartupPageType));
            _Locale = Read<string>(null, nameof(Locale));
            _OverrideIntractionMode = Read(default(Models.Pages.ApplicationInteractionMode?), nameof(OverrideIntractionMode));
            _Theme =  Read(ElementTheme.Default, nameof(AppTheme));
        }

        HohoemaPageType _StartupPageType;
        public HohoemaPageType StartupPageType
        {
            get => _StartupPageType;
            set => SetProperty(ref _StartupPageType, value);
        }

        string _Locale;
        public string Locale
        {
            get => _Locale;
            set => SetProperty(ref _Locale, value);
        }

        Models.Pages.ApplicationInteractionMode? _OverrideIntractionMode;
        public Models.Pages.ApplicationInteractionMode? OverrideIntractionMode
        {
            get => _OverrideIntractionMode;
            set => SetProperty(ref _OverrideIntractionMode, value);
        }

        AppearanceSettingsRepository.ElementTheme _Theme;
        public Windows.UI.Xaml.ElementTheme AppTheme
        {
            get => _Theme.ToXamlElementTheme();
            set => SetProperty(ref _Theme, value.ToAppElementTheme());
        }

        public enum ElementTheme
        {
            Default,
            Light,
            Dark
        }
    }



    public static class ElementThemeMapper
    {
        public static AppearanceSettingsRepository.ElementTheme ToAppElementTheme(this Windows.UI.Xaml.ElementTheme theme) => theme switch
        {
            Windows.UI.Xaml.ElementTheme.Default => AppearanceSettingsRepository.ElementTheme.Default,
            Windows.UI.Xaml.ElementTheme.Light => AppearanceSettingsRepository.ElementTheme.Light,
            Windows.UI.Xaml.ElementTheme.Dark => AppearanceSettingsRepository.ElementTheme.Dark,
            _ => throw new NotSupportedException()
        };

        public static Windows.UI.Xaml.ElementTheme ToXamlElementTheme(this AppearanceSettingsRepository.ElementTheme theme) => theme switch
        {
            AppearanceSettingsRepository.ElementTheme.Default => Windows.UI.Xaml.ElementTheme.Default,
            AppearanceSettingsRepository.ElementTheme.Light => Windows.UI.Xaml.ElementTheme.Light,
            AppearanceSettingsRepository.ElementTheme.Dark => Windows.UI.Xaml.ElementTheme.Dark,
            _ => throw new NotSupportedException()
        };
    }
}
