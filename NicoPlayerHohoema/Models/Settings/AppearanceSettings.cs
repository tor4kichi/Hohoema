using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;
using System.Runtime.Serialization;
using NicoPlayerHohoema.Services;
using System.Collections.Immutable;

namespace NicoPlayerHohoema.Models
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    [DataContract]
    public class AppearanceSettings : SettingsBase
    {
        [DataMember]
        public HohoemaPageType StartupPageType { get; set; } = HohoemaPageType.RankingCategoryList;

        [DataMember]
        public ApplicationIntaractionMode? OverrideIntractionMode { get; set; } = null;
    }

    public enum ApplicationIntaractionMode
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
