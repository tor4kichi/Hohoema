using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;

namespace NicoPlayerHohoema.Models
{
    [DataContract]
    public class PlaylistSettings : SettingsBase
    {
        private MediaPlaybackAutoRepeatMode _RepeatMode = MediaPlaybackAutoRepeatMode.List;

        [DataMember]
        public MediaPlaybackAutoRepeatMode RepeatMode
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


        private PlaylistEndAction _PlaylistEndAction;

        [DataMember]
        public PlaylistEndAction PlaylistEndAction
        {
            get { return _PlaylistEndAction; }
            set { SetProperty(ref _PlaylistEndAction, value); }
        }
    }


    public enum PlaylistEndAction
    {
        NothingDo,
        ChangeIntoSplit,
        CloseIfPlayWithCurrentWindow,
    }
}
