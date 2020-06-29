using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;
using System.Runtime.Serialization;
using Hohoema.Services;
using System.Collections.Immutable;
using Windows.UI.Xaml;
using Hohoema.Models.Pages;

namespace Hohoema.Models
{
    [Obsolete]
    [DataContract]
    public class AppearanceSettings : SettingsBase
    {
        [DataMember]
        public HohoemaPageType StartupPageType { get; set; } = HohoemaPageType.RankingCategoryList;

        [DataMember]
        public string Locale { get; set; }

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
