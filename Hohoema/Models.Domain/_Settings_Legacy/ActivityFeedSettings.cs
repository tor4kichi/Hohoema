using Hohoema.Models.Domain.Niconico.NicoRepo;
using Hohoema.Presentation.ViewModels;
using NiconicoToolkit.NicoRepo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Legacy
{
    [DataContract]
    public class ActivityFeedSettings : SettingsBase
    {
        [DataMember]
        public List<NicoRepoMuteContextTrigger> DisplayNicoRepoMuteContextTriggers { get; set; }

        public ActivityFeedSettings()
        {
            DisplayNicoRepoMuteContextTriggers = new List<NicoRepoMuteContextTrigger>();
        }
    }
}
