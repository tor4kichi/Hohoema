using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace NicoPlayerHohoema.Models
{
	[DataContract]
	public class PlayerSettings : SettingsBase
	{
		public static TimeSpan DefaultCommentDisplayDuration { get; private set; } = TimeSpan.FromSeconds(4);


		public PlayerSettings()
			: base()
		{
			DefaultQuality = NicoVideoQuality.Dmc_Midium;
			IsMute = false;
			SoundVolume = 0.5;
			SoundVolumeChangeFrequency = 0.05;
			IncrementReadablityOwnerComment = true;
			PauseWithCommentWriting = true;
			CommentRenderingFPS = 60;
			CommentDisplayDuration = DefaultCommentDisplayDuration;
			DefaultCommentFontScale = 1.0;
			CommentCommandPermission = CommentCommandPermissionType.Owner | CommentCommandPermissionType.User | CommentCommandPermissionType.Anonymous;
			CommentGlassMowerEnable = false;
			IsKeepDisplayInPlayback = true;
			IsKeepFrontsideInPlayback = true;
			IsDefaultCommentWithAnonymous = true;
			CommentColor = Colors.WhiteSmoke;
			IsAutoHidePlayerControlUI = true;
			AutoHidePlayerControlUIPreventTime = TimeSpan.FromSeconds(3);
			IsForceLandscape = false;
        }


		private NicoVideoQuality _DefaultQuality;

		[DataMember]
		public NicoVideoQuality DefaultQuality
		{
			get { return _DefaultQuality; }
			set
            {
                if (value == NicoVideoQuality.Dmc_SuperHigh) { return; }

                SetProperty(ref _DefaultQuality, value);
            }
		}


        private string _DefaultLiveQuality = null;

        [DataMember]
        public string DefaultLiveQuality
        {
            get { return _DefaultLiveQuality; }
            set { SetProperty(ref _DefaultLiveQuality, value); }
        }

        private bool _LiveWatchWithLowLatency = true;

        [DataMember]
        public bool LiveWatchWithLowLatency
        {
            get { return _LiveWatchWithLowLatency; }
            set { SetProperty(ref _LiveWatchWithLowLatency, value); }
        }


        private bool _IsMute;

		[DataMember]
		public bool IsMute
		{
			get { return _IsMute; }
			set { SetProperty(ref _IsMute, value); }
		}

		private double _SoundVolume;

		[DataMember]
		public double SoundVolume
		{
			get { return _SoundVolume; }
			set { SetProperty(ref _SoundVolume, value); }
		}

		private double _ScrollVolumeFrequency;

		[DataMember]
		public double SoundVolumeChangeFrequency
		{
			get { return _ScrollVolumeFrequency; }
			set { SetProperty(ref _ScrollVolumeFrequency, value); }
		}


		private PlayerDisplayMode _DisplayMode;

		[DataMember]
		public PlayerDisplayMode DisplayMode
		{
			get { return _DisplayMode; }
			set { SetProperty(ref _DisplayMode, value); }
		}


        
		private bool _IsCommentDisplay_Video = true;

		[DataMember]
		public bool CommentDisplay_Video
        {
			get { return _IsCommentDisplay_Video; }
			set { SetProperty(ref _IsCommentDisplay_Video, value); }
		}


        private bool _IsCommentDisplay_Live = true;

        [DataMember]
        public bool IsCommentDisplay_Live
        {
            get { return _IsCommentDisplay_Live; }
            set { SetProperty(ref _IsCommentDisplay_Live, value); }
        }



        private bool _IncrementReadablityOwnerComment;

		[DataMember]
		public bool IncrementReadablityOwnerComment
		{
			get { return _IncrementReadablityOwnerComment; }
			set { SetProperty(ref _IncrementReadablityOwnerComment, value); }
		}



		private bool _PauseWithCommentWriting;

		[DataMember]
		public bool PauseWithCommentWriting
		{
			get { return _PauseWithCommentWriting; }
			set { SetProperty(ref _PauseWithCommentWriting, value); }
		}


		private uint _CommentRenderingFPS;


		[DataMember]
		public uint CommentRenderingFPS
		{
			get { return _CommentRenderingFPS; }
			set { SetProperty(ref _CommentRenderingFPS, value); }
		}


		private TimeSpan _CommentDisplayDuration;


		[DataMember]
		public TimeSpan CommentDisplayDuration
		{
			get { return _CommentDisplayDuration; }
			set { SetProperty(ref _CommentDisplayDuration, value); }
		}



		private double _DefaultCommentFontScale;

		[DataMember]
		public double DefaultCommentFontScale
		{
			get { return _DefaultCommentFontScale; }
			set { SetProperty(ref _DefaultCommentFontScale, value); }
		}


        private CommentOpacityKind _CommentOpacityKind = CommentOpacityKind.NoSukesuke;

        [DataMember]
        public CommentOpacityKind CommentOpacity
        {
            get { return _CommentOpacityKind; }
            set { SetProperty(ref _CommentOpacityKind, value); }
        }




        private CommentCommandPermissionType _CommentCommandPermission;

		[DataMember]
		public CommentCommandPermissionType CommentCommandPermission
		{
			get { return _CommentCommandPermission; }
			set { SetProperty(ref _CommentCommandPermission, value); }
		}


		private bool _CommentGlassMowerEnable;

		[DataMember]
		public bool CommentGlassMowerEnable
		{
			get { return _CommentGlassMowerEnable; }
			set { SetProperty(ref _CommentGlassMowerEnable, value); }
		}



		private bool _IsKeepDisplayInPlayback;

		[DataMember]
		public bool IsKeepDisplayInPlayback
		{
			get { return _IsKeepDisplayInPlayback; }
			set { SetProperty(ref _IsKeepDisplayInPlayback, value); }
		}



		private bool _IsKeepFrontsideInPlayback;

		[DataMember]
		public bool IsKeepFrontsideInPlayback
		{
			get { return _IsKeepFrontsideInPlayback; }
			set { SetProperty(ref _IsKeepFrontsideInPlayback, value); }
		}


		private bool _IsDefaultCommentWithAnonymous;

		[DataMember]
		public bool IsDefaultCommentWithAnonymous
		{
			get { return _IsDefaultCommentWithAnonymous; }
			set { SetProperty(ref _IsDefaultCommentWithAnonymous, value); }
		}

		private Color _CommentColor;

		[DataMember]
		public Color CommentColor
		{
			get { return _CommentColor; }
			set { SetProperty(ref _CommentColor, value); }
		}



		private bool _IsAutoHidePlayerControlUI;

		[DataMember]
		public bool IsAutoHidePlayerControlUI
		{
			get { return _IsAutoHidePlayerControlUI; }
			set { SetProperty(ref _IsAutoHidePlayerControlUI, value); }
		}

		private TimeSpan _AutoHidePlayerControlUIPreventTime;

		[DataMember]
		public TimeSpan AutoHidePlayerControlUIPreventTime
		{
			get { return _AutoHidePlayerControlUIPreventTime; }
			set { SetProperty(ref _AutoHidePlayerControlUIPreventTime, value); }
		}


		private bool _IsForceLandscapeDefault;

		[DataMember]
		public bool IsForceLandscape
		{
			get { return _IsForceLandscapeDefault; }
			set { SetProperty(ref _IsForceLandscapeDefault, value); }
		}




        private double _PlaybackRate = 1.0;

        [DataMember]
        public double PlaybackRate
        {
            get { return _PlaybackRate; }
            set
            {
                var trimValue = Math.Min(2.0, Math.Max(0.1, value));
                SetProperty(ref _PlaybackRate, trimValue);
            }
        }


        
    }

    public enum VideoContentOpenAction
    {
        CurrentWindow,
        CurrentWindowWithSplit,
        //        NewWindow
    }

}
