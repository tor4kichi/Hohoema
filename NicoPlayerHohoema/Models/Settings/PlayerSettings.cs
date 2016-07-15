using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	[DataContract]
	public class PlayerSettings : SettingsBase
	{
		public PlayerSettings()
			: base()
		{
			SoundVolume = 0.25f;
			DefaultCommentDisplay = true;
			IncrementReadablityOwnerComment = true;
			PauseWithCommentWriting = true;
			CommentRenderingFPS = 24;
			DefaultCommentFontScale = 1.0f;
			CommentCommandPermission = CommentCommandPermissionType.Owner | CommentCommandPermissionType.User | CommentCommandPermissionType.Anonymous;
			CommentGlassMowerEnable = false;
			IsKeepDisplayInPlayback = true;
			IsKeepFrontsideInPlayback = true;
		}


		private bool _IsLowQualityDeafult;

		[DataMember]
		public bool IsLowQualityDeafult
		{
			get { return _IsLowQualityDeafult; }
			set { SetProperty(ref _IsLowQualityDeafult, value); }
		}


		private bool _IsMute;

		[DataMember]
		public bool IsMute
		{
			get { return _IsMute; }
			set { SetProperty(ref _IsMute, value); }
		}

		private float _SoundVolume;

		[DataMember]
		public float SoundVolume
		{
			get { return _SoundVolume; }
			set { SetProperty(ref _SoundVolume, value); }
		}


		private PlayerDisplayMode _DisplayMode;

		[DataMember]
		public PlayerDisplayMode DisplayMode
		{
			get { return _DisplayMode; }
			set { SetProperty(ref _DisplayMode, value); }
		}



		private bool _DefaultCommentDisplay;

		[DataMember]
		public bool DefaultCommentDisplay
		{
			get { return _DefaultCommentDisplay; }
			set { SetProperty(ref _DefaultCommentDisplay, value); }
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


		private float _DefaultCommentFontScale;

		[DataMember]
		public float DefaultCommentFontScale
		{
			get { return _DefaultCommentFontScale; }
			set { SetProperty(ref _DefaultCommentFontScale, value); }
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

	}
}
