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
    public class NicoRepoAndFeedSettings : SettingsBase
    {
        [DataMember]
        public List<NicoRepoItemTopic> DisplayNicoRepoItemTopics { get; set; } 


        public NicoRepoAndFeedSettings()
        {
            DisplayNicoRepoItemTopics = new List<NicoRepoItemTopic>();
        }
    }
}
