using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    [DataContract]
    public class PlaylistSettings : SettingsBase
    {
        private PlaybackMode _RepeatMode = PlaybackMode.Through;

        [DataMember]
        public PlaybackMode RepeatMode
        {
            get { return _RepeatMode; }
			set { SetProperty(ref _RepeatMode, value); }
        }


        private bool _IsShuffleEnable = false;

        [DataMember]
        public bool IsShuffleEnable
        {
            get { return _IsShuffleEnable; }
            set { SetProperty(ref _IsShuffleEnable, value); }
        }
    }
}
