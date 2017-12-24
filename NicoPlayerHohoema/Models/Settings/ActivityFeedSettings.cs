using NicoPlayerHohoema.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    [DataContract]
    public class ActivityFeedSettings : SettingsBase
    {
        [DataMember]
        public List<NicoRepoItemTopic> DisplayNicoRepoItemTopics { get; set; }



        private bool _IsLiveAlertEnabled = true;
        [DataMember]
        public bool IsLiveAlertEnabled
        {
            get { return _IsLiveAlertEnabled; }
            set { SetProperty(ref _IsLiveAlertEnabled, value); }
        }


        public ActivityFeedSettings()
        {
            DisplayNicoRepoItemTopics = new List<NicoRepoItemTopic>();
        }
    }
}
