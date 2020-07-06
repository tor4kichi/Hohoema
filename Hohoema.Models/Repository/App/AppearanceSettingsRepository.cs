using Hohoema.Models.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Hohoema.Models.Repository.App
{
    public sealed class AppearanceSettingsRepository : FlagsRepositoryBase
    {
        HohoemaPageType _StartupPageType = HohoemaPageType.RankingCategoryList;
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

        Models.Pages.ApplicationInteractionMode? _OverrideIntractionMode = null;
        public Models.Pages.ApplicationInteractionMode? OverrideIntractionMode
        {
            get => _OverrideIntractionMode;
            set => SetProperty(ref _OverrideIntractionMode, value);
        }

        ElementTheme _Theme = ElementTheme.Default;
        public ElementTheme Theme
        {
            get => _Theme;
            set => SetProperty(ref _Theme, value);
        }


    }
}
