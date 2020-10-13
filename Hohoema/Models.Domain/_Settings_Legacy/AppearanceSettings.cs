using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;
using System.Runtime.Serialization;
using Hohoema.Presentation.Services;
using System.Collections.Immutable;
using Windows.UI.Xaml;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Application;

namespace Hohoema.Models.Domain.Legacy
{
    [DataContract]
    public class AppearanceSettings : SettingsBase
    {
        [DataMember]
        public string Locale { get; set; } = I18NPortable.I18N.Current.GetDefaultLocale();

        [DataMember]
        public HohoemaPageType FirstAppearPageType { get; set; } = HohoemaPageType.RankingCategoryList;

        [DataMember]
        public ApplicationInteractionMode? OverrideIntractionMode { get; set; } = null;

        [DataMember]
        public ElementTheme Theme { get; set; } = ElementTheme.Default;
    }

}
