using Hohoema.Models.Niconico.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI;

namespace Hohoema.Models.Repository.App
{
    public sealed class PlayerSettingsRepository : FlagsRepositoryBase
    {
		public PlayerSettingsRepository()
        {
			_DefaultQuality = Read(NicoVideoQuality.Dmc_Midium, nameof(DefaultQuality));
			_IsMute = Read(false, nameof(IsMute));
			_SoundVolume = Read(1.0, nameof(SoundVolume));
			_SoundVolumeChangeFrequency = Read(0.05, nameof(SoundVolumeChangeFrequency));
			_IsLoudnessCorrectionEnabled = Read(true, nameof(IsLoudnessCorrectionEnabled));
			_IsCommentDisplay_Video = Read(true, nameof(IsCommentDisplay_Video));
			_PauseWithCommentWriting = Read(false, nameof(PauseWithCommentWriting));
			_CommentDisplayDuration = Read(TimeSpan.FromSeconds(4), nameof(CommentDisplayDuration));
			_DefaultCommentFontScale = Read(1.0, nameof(DefaultCommentFontScale));
			_CommentOpacity = Read(1.0, nameof(CommentOpacity));
			_IsDefaultCommentWithAnonymous = Read(true, nameof(IsDefaultCommentWithAnonymous));
			_CommentColor = Read(Colors.White, nameof(CommentColor));
			_AutoHidePlayerControlUIPreventTime = Read(TimeSpan.FromSeconds(4), nameof(AutoHidePlayerControlUIPreventTime));
			_IsForceLandscape = Read(false, nameof(IsForceLandscape));
			_PlaybackRate = Read(1.0, nameof(PlaybackRate));
			_NicoScript_DisallowSeek_Enabled = Read(true, nameof(NicoScript_DisallowSeek_Enabled));
			_NicoScript_Default_Enabled = Read(true, nameof(NicoScript_Default_Enabled));
			_NicoScript_Jump_Enabled = Read(true, nameof(NicoScript_Jump_Enabled));
			_NicoScript_DisallowComment_Enabled = Read(true, nameof(NicoScript_DisallowComment_Enabled));
			_NicoScript_Replace_Enabled = Read(true, nameof(NicoScript_Replace_Enabled));
			_IsCurrentVideoLoopingEnabled = Read(false, nameof(IsCurrentVideoLoopingEnabled));
			_IsPlaylistLoopingEnabled = Read(false, nameof(IsPlaylistLoopingEnabled));
			_IsShuffleEnable = Read(false, nameof(IsShuffleEnable));
			_IsReverseModeEnable = Read(false, nameof(IsReverseModeEnable));
			_PlaylistEndAction = Read(Playlist.PlaylistEndAction.NothingDo, nameof(PlaylistEndAction));
			_AutoMoveNextVideoOnPlaylistEmpty = Read(true, nameof(AutoMoveNextVideoOnPlaylistEmpty));
		}


		private NicoVideoQuality _DefaultQuality;
		public NicoVideoQuality DefaultQuality
		{
			get { return _DefaultQuality; }
			set { SetProperty(ref _DefaultQuality, value); }			
		}


        #region Sound

        private bool _IsMute;
		public bool IsMute
		{
			get { return _IsMute; }
			set { SetProperty(ref _IsMute, value); }
		}


        private double _SoundVolume;
		public double SoundVolume
		{
			get { return _SoundVolume; }
			set { SetProperty(ref _SoundVolume, Math.Min(1.0, Math.Max(0.0, value))); }
		}






		private double _SoundVolumeChangeFrequency;
		public double SoundVolumeChangeFrequency
		{
			get { return _SoundVolumeChangeFrequency; }
			set { SetProperty(ref _SoundVolumeChangeFrequency, Math.Max(0.001, value)); }
		}


		private bool _IsLoudnessCorrectionEnabled;
		public bool IsLoudnessCorrectionEnabled
		{
			get { return _IsLoudnessCorrectionEnabled; }
			set { SetProperty(ref _IsLoudnessCorrectionEnabled, value); }
		}


        #endregion

        
		private bool _IsCommentDisplay_Video;
		public bool IsCommentDisplay_Video
        {
			get { return _IsCommentDisplay_Video; }
			set { SetProperty(ref _IsCommentDisplay_Video, value); }
		}



		private bool _PauseWithCommentWriting;
		public bool PauseWithCommentWriting
		{
			get { return _PauseWithCommentWriting; }
			set { SetProperty(ref _PauseWithCommentWriting, value); }
		}




		private TimeSpan _CommentDisplayDuration;
		public TimeSpan CommentDisplayDuration
		{
			get { return _CommentDisplayDuration; }
			set { SetProperty(ref _CommentDisplayDuration, value); }
		}



		private double _DefaultCommentFontScale;
		public double DefaultCommentFontScale
		{
			get { return _DefaultCommentFontScale; }
			set { SetProperty(ref _DefaultCommentFontScale, value); }
		}


        private double _CommentOpacity;
        public double CommentOpacity
        {
            get { return _CommentOpacity; }
            set
            {
                SetProperty(ref _CommentOpacity, Math.Min(1.0, Math.Max(0.0, value)));
            }
        }



		private bool _IsDefaultCommentWithAnonymous;
		public bool IsDefaultCommentWithAnonymous
		{
			get { return _IsDefaultCommentWithAnonymous; }
			set { SetProperty(ref _IsDefaultCommentWithAnonymous, value); }
		}

		private Color _CommentColor;
		public Color CommentColor
		{
			get { return _CommentColor; }
			set { SetProperty(ref _CommentColor, value); }
		}

		private TimeSpan _AutoHidePlayerControlUIPreventTime;
		public TimeSpan AutoHidePlayerControlUIPreventTime
		{
			get { return _AutoHidePlayerControlUIPreventTime; }
			set { SetProperty(ref _AutoHidePlayerControlUIPreventTime, value); }
		}


		private bool _IsForceLandscape;
		public bool IsForceLandscape
		{
			get { return _IsForceLandscape; }
			set { SetProperty(ref _IsForceLandscape, value); }
		}

		private double _PlaybackRate;
        public double PlaybackRate
        {
            get { return _PlaybackRate; }
            set
            {
                var trimValue = Math.Min(2.0, Math.Max(0.1, value));
                SetProperty(ref _PlaybackRate, trimValue);
            }
        }






		public bool _NicoScript_DisallowSeek_Enabled;
		public bool NicoScript_DisallowSeek_Enabled
		{
			get { return _NicoScript_DisallowSeek_Enabled; }
			set { SetProperty(ref _NicoScript_DisallowSeek_Enabled, value); }
		}

		public bool _NicoScript_Default_Enabled;
		public bool NicoScript_Default_Enabled
		{
			get { return _NicoScript_Default_Enabled; }
			set { SetProperty(ref _NicoScript_Default_Enabled, value); }
		}

		public bool _NicoScript_Jump_Enabled;
		public bool NicoScript_Jump_Enabled
		{
			get { return _NicoScript_Jump_Enabled; }
			set { SetProperty(ref _NicoScript_Jump_Enabled, value); }
		}


		public bool _NicoScript_DisallowComment_Enabled;
		public bool NicoScript_DisallowComment_Enabled
		{
			get { return _NicoScript_DisallowComment_Enabled; }
			set { SetProperty(ref _NicoScript_DisallowComment_Enabled, value); }
		}


		public bool _NicoScript_Replace_Enabled;
		public bool NicoScript_Replace_Enabled
		{
			get { return _NicoScript_Replace_Enabled; }
			set { SetProperty(ref _NicoScript_Replace_Enabled, value); }
		}



		private bool _IsCurrentVideoLoopingEnabled;
		public bool IsCurrentVideoLoopingEnabled
		{
			get { return _IsCurrentVideoLoopingEnabled; }
			set { SetProperty(ref _IsCurrentVideoLoopingEnabled, value); }
		}

		private bool _IsPlaylistLoopingEnabled;
		public bool IsPlaylistLoopingEnabled
		{
			get { return _IsPlaylistLoopingEnabled; }
			set { SetProperty(ref _IsPlaylistLoopingEnabled, value); }
		}



		private bool _IsShuffleEnable;
		public bool IsShuffleEnable
		{
			get { return _IsShuffleEnable; }
			set { SetProperty(ref _IsShuffleEnable, value); }
		}


		private bool _IsReverseModeEnable;
		public bool IsReverseModeEnable
		{
			get { return _IsReverseModeEnable; }
			set { SetProperty(ref _IsReverseModeEnable, value); }
		}



		private Models.Repository.Playlist.PlaylistEndAction _PlaylistEndAction;
		public Models.Repository.Playlist.PlaylistEndAction PlaylistEndAction
		{
			get { return _PlaylistEndAction; }
			set { SetProperty(ref _PlaylistEndAction, value); }
		}


		private bool _AutoMoveNextVideoOnPlaylistEmpty;
		public bool AutoMoveNextVideoOnPlaylistEmpty
		{
			get { return _AutoMoveNextVideoOnPlaylistEmpty; }
			set { SetProperty(ref _AutoMoveNextVideoOnPlaylistEmpty, value); }
		}
    }
}
