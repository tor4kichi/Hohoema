using Hohoema.Models.Niconico.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI;

namespace Hohoema.Models.Repository.Playlist
{
    public sealed class PlayerSettingsRepository : FlagsRepositoryBase
    {
		private NicoVideoQuality _DefaultQuality = NicoVideoQuality.Dmc_Midium;
		public NicoVideoQuality DefaultQuality
		{
			get { return _DefaultQuality; }
			set { SetProperty(ref _DefaultQuality, value); }			
		}


        private string _DefaultLiveQuality = null;
        public string DefaultLiveQuality
        {
            get { return _DefaultLiveQuality; }
            set { SetProperty(ref _DefaultLiveQuality, value); }
        }

        private bool _LiveWatchWithLowLatency = true;
        public bool LiveWatchWithLowLatency
        {
            get { return _LiveWatchWithLowLatency; }
            set { SetProperty(ref _LiveWatchWithLowLatency, value); }
        }


        #region Sound

        private bool _IsMute = false;
		public bool IsMute
		{
			get { return _IsMute; }
			set { SetProperty(ref _IsMute, value); }
		}


        private double _SoundVolume = 1.0;
		public double SoundVolume
		{
			get { return _SoundVolume; }
			set { SetProperty(ref _SoundVolume, Math.Min(1.0, Math.Max(0.0, value))); }
		}






		private double _SoundVolumeChangeFrequency = 0.05;
		public double SoundVolumeChangeFrequency
		{
			get { return _SoundVolumeChangeFrequency; }
			set { SetProperty(ref _SoundVolumeChangeFrequency, Math.Max(0.001, value)); }
		}


		private bool _IsLoudnessCorrectionEnabled = true;
		public bool IsLoudnessCorrectionEnabled
		{
			get { return _IsLoudnessCorrectionEnabled; }
			set { SetProperty(ref _IsLoudnessCorrectionEnabled, value); }
		}


        #endregion

        
		private bool _IsCommentDisplay_Video = true;
		public bool IsCommentDisplay_Video
        {
			get { return _IsCommentDisplay_Video; }
			set { SetProperty(ref _IsCommentDisplay_Video, value); }
		}


        private bool _IsCommentDisplay_Live = true;
        public bool IsCommentDisplay_Live
        {
            get { return _IsCommentDisplay_Live; }
            set { SetProperty(ref _IsCommentDisplay_Live, value); }
        }



		private bool _PauseWithCommentWriting = false;
		public bool PauseWithCommentWriting
		{
			get { return _PauseWithCommentWriting; }
			set { SetProperty(ref _PauseWithCommentWriting, value); }
		}




		private TimeSpan _CommentDisplayDuration = TimeSpan.FromSeconds(4);
		public TimeSpan CommentDisplayDuration
		{
			get { return _CommentDisplayDuration; }
			set { SetProperty(ref _CommentDisplayDuration, value); }
		}



		private double _DefaultCommentFontScale = 1.0;
		public double DefaultCommentFontScale
		{
			get { return _DefaultCommentFontScale; }
			set { SetProperty(ref _DefaultCommentFontScale, value); }
		}


        private double _CommentOpacity = 1.0;
        public double CommentOpacity
        {
            get { return _CommentOpacity; }
            set
            {
                SetProperty(ref _CommentOpacity, Math.Min(1.0, Math.Max(0.0, value)));
            }
        }



		private bool _IsDefaultCommentWithAnonymous = true;
		public bool IsDefaultCommentWithAnonymous
		{
			get { return _IsDefaultCommentWithAnonymous; }
			set { SetProperty(ref _IsDefaultCommentWithAnonymous, value); }
		}

		private Color _CommentColor = Colors.White;
		public Color CommentColor
		{
			get { return _CommentColor; }
			set { SetProperty(ref _CommentColor, value); }
		}

		private TimeSpan _AutoHidePlayerControlUIPreventTime = TimeSpan.FromSeconds(4);
		public TimeSpan AutoHidePlayerControlUIPreventTime
		{
			get { return _AutoHidePlayerControlUIPreventTime; }
			set { SetProperty(ref _AutoHidePlayerControlUIPreventTime, value); }
		}


		private bool _IsForceLandscape = false;
		public bool IsForceLandscape
		{
			get { return _IsForceLandscape; }
			set { SetProperty(ref _IsForceLandscape, value); }
		}

		private double _PlaybackRate = 1.0;
        public double PlaybackRate
        {
            get { return _PlaybackRate; }
            set
            {
                var trimValue = Math.Min(2.0, Math.Max(0.1, value));
                SetProperty(ref _PlaybackRate, trimValue);
            }
        }






		public bool _NicoScript_DisallowSeek_Enabled = true;
		public bool NicoScript_DisallowSeek_Enabled
		{
			get { return _NicoScript_DisallowSeek_Enabled; }
			set { SetProperty(ref _NicoScript_DisallowSeek_Enabled, value); }
		}

		public bool _NicoScript_Default_Enabled = true;
		public bool NicoScript_Default_Enabled
		{
			get { return _NicoScript_Default_Enabled; }
			set { SetProperty(ref _NicoScript_Default_Enabled, value); }
		}

		public bool _NicoScript_Jump_Enabled = true;
		public bool NicoScript_Jump_Enabled
		{
			get { return _NicoScript_Jump_Enabled; }
			set { SetProperty(ref _NicoScript_Jump_Enabled, value); }
		}


		public bool _NicoScript_DisallowComment_Enabled = true;
		public bool NicoScript_DisallowComment_Enabled
		{
			get { return _NicoScript_DisallowComment_Enabled; }
			set { SetProperty(ref _NicoScript_DisallowComment_Enabled, value); }
		}


		public bool _NicoScript_Replace_Enabled = true;
		public bool NicoScript_Replace_Enabled
		{
			get { return _NicoScript_Replace_Enabled; }
			set { SetProperty(ref _NicoScript_Replace_Enabled, value); }
		}



		private bool _isCurrentVideoLoopingEnabled = false;
		public bool IsCurrentVideoLoopingEnabled
		{
			get { return _isCurrentVideoLoopingEnabled; }
			set { SetProperty(ref _isCurrentVideoLoopingEnabled, value); }
		}

		private bool _isPlaylistLoopingEnabled = false;
		public bool IsPlaylistLoopingEnabled
		{
			get { return _isPlaylistLoopingEnabled; }
			set { SetProperty(ref _isPlaylistLoopingEnabled, value); }
		}



		private bool _IsShuffleEnable = false;
		public bool IsShuffleEnable
		{
			get { return _IsShuffleEnable; }
			set { SetProperty(ref _IsShuffleEnable, value); }
		}


		private bool _IsReverseModeEnable = false;
		public bool IsReverseModeEnable
		{
			get { return _IsReverseModeEnable; }
			set { SetProperty(ref _IsReverseModeEnable, value); }
		}



		private PlaylistEndAction _PlaylistEndAction;
		public PlaylistEndAction PlaylistEndAction
		{
			get { return _PlaylistEndAction; }
			set { SetProperty(ref _PlaylistEndAction, value); }
		}


		private bool _AutoMoveNextVideoOnPlaylistEmpty = true;
		public bool AutoMoveNextVideoOnPlaylistEmpty
		{
			get { return _AutoMoveNextVideoOnPlaylistEmpty; }
			set { SetProperty(ref _AutoMoveNextVideoOnPlaylistEmpty, value); }
		}
    }
}
