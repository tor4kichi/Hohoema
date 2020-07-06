using Hohoema.Models.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models
{
    [Obsolete]
    [DataContract]
    public sealed class PinSettings : SettingsBase
    {
        [DataMember]
        public ObservableCollection<HohoemaPin> Pins { get; } = new ObservableCollection<HohoemaPin>();
    }

    [Obsolete]
    [DataContract]
    public sealed class HohoemaPin : FixPrism.BindableBase
    {
        [DataMember]
        public HohoemaPageType PageType { get; set; }
        [DataMember]
        public string Parameter { get; set; }
        [DataMember]
        public string Label { get; set; }

        private string _OverrideLabel;
        [DataMember]
        public string OverrideLabel
        {
            get { return _OverrideLabel; }
            set { SetProperty(ref _OverrideLabel, value); }
        }
    }
}
