using Microsoft.Toolkit.Uwp.Helpers;
using NicoPlayerHohoema.FixPrism;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Media;
using Windows.UI;

namespace NicoPlayerHohoema.Models
{

	public class LiveNGUserInfo
	{
		static readonly TimeSpan OUTDATE_TIME = TimeSpan.FromDays(7);
		public string UserId { get; set; }
		public string ScreenName { get; set; }
		public bool IsAnonimity => int.TryParse(UserId, out var _);
		public DateTime AddedAt { get; set; } = DateTime.Now;

		public bool IsOutDated => IsAnonimity && (DateTime.Now - AddedAt > OUTDATE_TIME);
	}



	public enum PlaylistEndAction
	{
		NothingDo,
		ChangeIntoSplit,
		CloseIfPlayWithCurrentWindow,
	}

	[DataContract]
	public class PlayerSettings : SettingsBase
	{
		public static TimeSpan DefaultCommentDisplayDuration { get; private set; } = TimeSpan.FromSeconds(4);

		public PlayerSettings()
		{
			DefaultQuality = NicoVideoQuality.Dmc_Midium;
			IsMute = false;
			SoundVolume = 0.5;
			SoundVolumeChangeFrequency = 0.05;
			IncrementReadablityOwnerComment = true;
			PauseWithCommentWriting = false;
			CommentRenderingFPS = 60;
			CommentDisplayDuration = DefaultCommentDisplayDuration;
			DefaultCommentFontScale = 1.0;
			CommentGlassMowerEnable = false;
			IsKeepDisplayInPlayback = true;
			IsKeepFrontsideInPlayback = true;
			IsDefaultCommentWithAnonymous = true;
			CommentColor = Colors.WhiteSmoke;
			IsAutoHidePlayerControlUI = true;
			AutoHidePlayerControlUIPreventTime = TimeSpan.FromSeconds(3);
			IsForceLandscape = false;

			NGCommentUserIdEnable = true;
			NGCommentUserIds = new ObservableCollection<UserIdInfo>();
			NGCommentKeywordEnable = true;
			NGCommentKeywords = new ObservableCollection<NGKeyword>();
			NGCommentScore = -1000;
		}

		protected void Reset(PlayerSettings read)
		{
			DefaultQuality = read.DefaultQuality;
			DefaultLiveQuality = read.DefaultLiveQuality;
			LiveWatchWithLowLatency = read.LiveWatchWithLowLatency;
			IsMute = read.IsMute;
			SoundVolume = read.SoundVolume;
			SoundVolumeChangeFrequency = read.SoundVolumeChangeFrequency;
			IncrementReadablityOwnerComment = read.IncrementReadablityOwnerComment;
			IsCommentDisplay_Video = read.IsCommentDisplay_Video;
			IsCommentDisplay_Live = read.IsCommentDisplay_Live;
			PauseWithCommentWriting = read.PauseWithCommentWriting;
			CommentRenderingFPS = read.CommentRenderingFPS;
			CommentDisplayDuration = read.CommentDisplayDuration;
			CommentOpacity = read.CommentOpacity;
			DefaultCommentFontScale = read.DefaultCommentFontScale;
			CommentGlassMowerEnable = read.CommentGlassMowerEnable;
			IsKeepDisplayInPlayback = read.IsKeepDisplayInPlayback;
			IsKeepFrontsideInPlayback = read.IsKeepFrontsideInPlayback;
			IsDefaultCommentWithAnonymous = read.IsDefaultCommentWithAnonymous;
			CommentColor = read.CommentColor;
			PlaybackRate = read.PlaybackRate;
			IsAutoHidePlayerControlUI = read.IsAutoHidePlayerControlUI;
			AutoHidePlayerControlUIPreventTime = read.AutoHidePlayerControlUIPreventTime;
			IsForceLandscape = read.IsForceLandscape;

			NicoScript_DisallowSeek_Enabled = read.NicoScript_DisallowSeek_Enabled;
			NicoScript_Default_Enabled = read.NicoScript_Default_Enabled;
			NicoScript_DisallowComment_Enabled = read.NicoScript_DisallowComment_Enabled;
			NicoScript_Jump_Enabled = read.NicoScript_Jump_Enabled;
			NicoScript_Replace_Enabled = read.NicoScript_Replace_Enabled;

			NGCommentUserIdEnable = read.NGCommentUserIdEnable;
			NGCommentUserIds.Clear();
			foreach (var id in read.NGCommentUserIds)
			{
				NGCommentUserIds.Add(id);
			}

			NGCommentKeywordEnable = read.NGCommentKeywordEnable;
			NGCommentKeywords.Clear();
			foreach (var keyword in read.NGCommentKeywords)
			{
				NGCommentKeywords.Add(keyword);
			}
			NGCommentScore = read.NGCommentScore;

			IsNGLiveCommentUserEnable = read.IsNGLiveCommentUserEnable;
			NGLiveCommentUserIds.Clear();
			foreach (var id in read.NGLiveCommentUserIds)
			{
				NGLiveCommentUserIds.Add(id);
			}

			RepeatMode = read.RepeatMode;
			IsShuffleEnable = read.IsShuffleEnable;
			IsReverseModeEnable = read.IsReverseModeEnable;
			PlaylistEndAction = read.PlaylistEndAction;
			AutoMoveNextVideoOnPlaylistEmpty = read.AutoMoveNextVideoOnPlaylistEmpty;
		}




		private NicoVideoQuality _DefaultQuality;

		[DataMember]
		public NicoVideoQuality DefaultQuality
		{
			get { return _DefaultQuality; }
			set { SetProperty(ref _DefaultQuality, value); }			
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


        #region Sound

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
			set { SetProperty(ref _SoundVolume, Math.Min(1.0, Math.Max(0.0, value))); }
		}






		private double _SoundVolumeChangeFrequency;

		[DataMember]
		public double SoundVolumeChangeFrequency
		{
			get { return _SoundVolumeChangeFrequency; }
			set { SetProperty(ref _SoundVolumeChangeFrequency, Math.Max(0.001f, value)); }
		}


		private bool _IsLoudnessCorrectionEnabled = true;

		[DataMember]
		public bool IsLoudnessCorrectionEnabled
		{
			get { return _IsLoudnessCorrectionEnabled; }
			set { SetProperty(ref _IsLoudnessCorrectionEnabled, value); }
		}


        #endregion

        
		private bool _IsCommentDisplay_Video = true;

		[DataMember]
		public bool IsCommentDisplay_Video
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


        private double _CommentOpacity = 1.0;

        [DataMember]
        public double CommentOpacity
        {
            get { return _CommentOpacity; }
            set
            {
                SetProperty(ref _CommentOpacity, Math.Min(1.0, Math.Max(0.0, value)));
            }
        }



		private bool _CommentGlassMowerEnable;

		[DataMember]
		public bool CommentGlassMowerEnable
		{
			get { return _CommentGlassMowerEnable; }
			set { SetProperty(ref _CommentGlassMowerEnable, value); }
		}

		Regex _glassRegex = new Regex("([wWｗＷ]){2,}");
		public bool TryGlassMower(string comment, out string glassRemoved)
		{
			if (_glassRegex.IsMatch(comment))
			{
				glassRemoved = _glassRegex.Replace(comment, "ｗ");
				return true;
			}
			else
			{
				glassRemoved = comment;
				return false;
			}
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


		private bool _IsForceLandscape;

		[DataMember]
		public bool IsForceLandscape
		{
			get { return _IsForceLandscape; }
			set { SetProperty(ref _IsForceLandscape, value); }
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


        private DelegateCommand<double?> _SetPlaybackRateCommand;
        public DelegateCommand<double?> SetPlaybackRateCommand
        {
            get
            {
                return _SetPlaybackRateCommand
                    ?? (_SetPlaybackRateCommand = new DelegateCommand<double?>(
                        (rate) => PlaybackRate = rate.HasValue ? rate.Value : 1.0
                        , (rate) => rate.HasValue ? rate.Value != PlaybackRate : true)
                        );
            }
        }

        public bool _NicoScript_DisallowSeek_Enabled = true;

        [DataMember]
        public bool NicoScript_DisallowSeek_Enabled
        {
            get { return _NicoScript_DisallowSeek_Enabled; }
            set { SetProperty(ref _NicoScript_DisallowSeek_Enabled, value); }
        }

        public bool _NicoScript_Default_Enabled = true;

        [DataMember]
        public bool NicoScript_Default_Enabled
        {
            get { return _NicoScript_Default_Enabled; }
            set { SetProperty(ref _NicoScript_Default_Enabled, value); }
        }

        public bool _NicoScript_Jump_Enabled = true;

        [DataMember]
        public bool NicoScript_Jump_Enabled
        {
            get { return _NicoScript_Jump_Enabled; }
            set { SetProperty(ref _NicoScript_Jump_Enabled, value); }
        }


        public bool _NicoScript_DisallowComment_Enabled = true;

        [DataMember]
        public bool NicoScript_DisallowComment_Enabled
        {
            get { return _NicoScript_DisallowComment_Enabled; }
            set { SetProperty(ref _NicoScript_DisallowComment_Enabled, value); }
        }


        public bool _NicoScript_Replace_Enabled = true;

        [DataMember]
        public bool NicoScript_Replace_Enabled
        {
            get { return _NicoScript_Replace_Enabled; }
            set { SetProperty(ref _NicoScript_Replace_Enabled, value); }
        }


		#region Comment NG


		private bool _NGCommentUserIdEnable;

		[DataMember]
		public bool NGCommentUserIdEnable
		{
			get { return _NGCommentUserIdEnable; }
			set { SetProperty(ref _NGCommentUserIdEnable, value); }
		}

		[DataMember]
		public ObservableCollection<UserIdInfo> NGCommentUserIds { get; private set; }

		private bool _NGCommentKeywordEnable;

		[DataMember]
		public bool NGCommentKeywordEnable
		{
			get { return _NGCommentKeywordEnable; }
			set { SetProperty(ref _NGCommentKeywordEnable, value); }
		}

		[DataMember]
		public ObservableCollection<NGKeyword> NGCommentKeywords { get; private set; }




		private int _NGCommentScore;

		[DataMember]
		public int NGCommentScore
		{
			get { return _NGCommentScore; }
			set { SetProperty(ref _NGCommentScore, value); }
		}




		public NGResult IsNGCommentUser(string userId)
		{
			if (this.NGCommentUserIdEnable && this.NGCommentUserIds.Count > 0)
			{
				var ngItem = this.NGCommentUserIds.FirstOrDefault(x => x.UserId == userId);

				if (ngItem != null)
				{
					return new NGResult()
					{
						NGReason = NGReason.UserId,
						Content = userId.ToString(),
						NGDescription = ngItem.Description,
					};

				}
			}

			return null;
		}

		public NGResult IsNGComment(string commentText)
		{
			if (this.NGCommentKeywordEnable && this.NGCommentKeywords.Count > 0)
			{
				var ngItem = this.NGCommentKeywords.FirstOrDefault(x => x.CheckNG(commentText));

				if (ngItem != null)
				{
					return new NGResult()
					{
						NGReason = NGReason.Keyword,
						Content = ngItem.Keyword,
					};

				}
			}

			return null;
		}


		bool _IsCommentCommandFilteringEnable = true;

		[DataMember]
		public bool IsCommentCommandFilteringEnable
		{
			get { return _IsCommentCommandFilteringEnable; }
			set { SetProperty(ref _IsCommentCommandFilteringEnable, value); }
		}

		[DataMember]
		public ObservableHashSet<string> FilteringCommands { get; private set; } = new ObservableHashSet<string>() { "naka", "center" };




		#endregion



		private bool _NGLiveCommentUserEnable = true;

		[DataMember]
		public bool IsNGLiveCommentUserEnable
		{
			get { return _NGLiveCommentUserEnable; }
			set { SetProperty(ref _NGLiveCommentUserEnable, value); }
		}

		[DataMember]
		public ObservableCollection<LiveNGUserInfo> NGLiveCommentUserIds { get; private set; } = new ObservableCollection<LiveNGUserInfo>();

		public void AddNGLiveCommentUserId(string userId, string screenName)
		{
			NGLiveCommentUserIds.Add(new LiveNGUserInfo()
			{
				UserId = userId,
				ScreenName = screenName,
				AddedAt = DateTime.Now,
			});
		}
		public void RemoveNGLiveCommentUserId(string userId)
		{
			var ngUser = NGLiveCommentUserIds.FirstOrDefault(x => x.UserId == userId);
			if (ngUser != null)
			{
				NGLiveCommentUserIds.Remove(ngUser);
			}
		}

		public void RemoveOutdatedLiveCommentNGUserIds()
		{
			foreach (var ngUserInfo in NGLiveCommentUserIds.Where(x => x.IsOutDated).ToArray())
			{
				NGLiveCommentUserIds.Remove(ngUserInfo);
			}
		}

		public bool IsLiveNGComment(string userId)
		{
			if (userId == null) { return false; }
			return NGLiveCommentUserIds.Any(x => x.UserId == userId);
		}




		#region Playlist


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


		private bool _IsReverseModeEnable = false;

		[DataMember]
		public bool IsReverseModeEnable
		{
			get { return _IsReverseModeEnable; }
			set { SetProperty(ref _IsReverseModeEnable, value); }
		}



		private PlaylistEndAction _PlaylistEndAction;

		[DataMember]
		public PlaylistEndAction PlaylistEndAction
		{
			get { return _PlaylistEndAction; }
			set { SetProperty(ref _PlaylistEndAction, value); }
		}


		private bool _AutoMoveNextVideoOnPlaylistEmpty = true;

		[DataMember]
		public bool AutoMoveNextVideoOnPlaylistEmpty
		{
			get { return _AutoMoveNextVideoOnPlaylistEmpty; }
			set { SetProperty(ref _AutoMoveNextVideoOnPlaylistEmpty, value); }
		}

		private DelegateCommand _ToggleRepeatModeCommand;
		public DelegateCommand ToggleRepeatModeCommand
		{
			get
			{
				return _ToggleRepeatModeCommand
					?? (_ToggleRepeatModeCommand = new DelegateCommand(() =>
					{
						switch (RepeatMode)
						{
							case MediaPlaybackAutoRepeatMode.None:
								RepeatMode = MediaPlaybackAutoRepeatMode.Track;
								break;
							case MediaPlaybackAutoRepeatMode.Track:
								RepeatMode = MediaPlaybackAutoRepeatMode.List;
								break;
							case MediaPlaybackAutoRepeatMode.List:
								RepeatMode = MediaPlaybackAutoRepeatMode.None;
								break;
							default:
								break;
						}
					}
					));
			}
		}

		private DelegateCommand _ToggleShuffleCommand;
		public DelegateCommand ToggleShuffleCommand
		{
			get
			{
				return _ToggleShuffleCommand
					?? (_ToggleShuffleCommand = new DelegateCommand(() =>
					{
						IsShuffleEnable = !IsShuffleEnable;
					}
					));
			}
		}


		#endregion
	}
}
