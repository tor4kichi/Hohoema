using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;
using System.Runtime.Serialization;
using NicoPlayerHohoema.Services;
using System.Collections.Immutable;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Models
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    [DataContract]
    public class AppearanceSettings : SettingsBase
    {
        [DataMember]
        public HohoemaPageType StartupPageType { get; set; } = HohoemaPageType.RankingCategoryList;

        [DataMember]
        public string Locale { get; set; } = I18NPortable.I18N.Current.GetDefaultLocale();

        [DataMember]
        public ApplicationInteractionMode? OverrideIntractionMode { get; set; } = null;

        [DataMember]
        public ElementTheme Theme { get; set; } = ElementTheme.Default;
    }

    public enum ApplicationInteractionMode
    {
        Controller,
        Mouse,
        Touch,
    }


    public enum ApplicationLayout
    {
        TV,
        Desktop,
        Tablet,
        Mobile,
    }
}
