﻿using Hohoema.Models.Domain.Pins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Legacy
{
    [DataContract]
    public sealed class PinSettings : SettingsBase
    {
        [DataMember]
        public ObservableCollection<HohoemaPin> Pins { get; } = new ObservableCollection<HohoemaPin>();
    }
}
