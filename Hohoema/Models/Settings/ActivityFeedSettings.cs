﻿using Hohoema.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models
{
    [DataContract]
    public class ActivityFeedSettings : SettingsBase
    {
        [DataMember]
        public List<NicoRepoItemTopic> DisplayNicoRepoItemTopics { get; set; }

        public ActivityFeedSettings()
        {
            DisplayNicoRepoItemTopics = new List<NicoRepoItemTopic>();
        }
    }
}
