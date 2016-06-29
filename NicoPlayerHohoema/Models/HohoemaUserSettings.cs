using Mntone.Nico2.Videos.Ranking;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Windows.Storage;
using Newtonsoft.Json;
using System.IO;
using NicoPlayerHohoema.Util;
using Mntone.Nico2.Videos.Thumbnail;

namespace NicoPlayerHohoema.Models
{
	public class HohoemaUserSettings
	{
		const string AccountSettingsFileName = "account.json";
		const string RankingSettingsFileName = "ranking.json";
		const string PlayerSettingsFileName = "player.json";
		const string NGSettingsFileName = "ng.json";


		public static async Task<HohoemaUserSettings> LoadSettings()
		{
			var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;


			var account = await SettingsBase.Load<AccountSettings>(AccountSettingsFileName);
			var ranking = await SettingsBase.Load<RankingSettings>(RankingSettingsFileName);
			var player = await SettingsBase.Load<PlayerSettings>(PlayerSettingsFileName);
			var ng = await SettingsBase.Load<NGSettings>(NGSettingsFileName);

			return new HohoemaUserSettings()
			{
				AccontSettings = account,
				RankingSettings = ranking,
				PlayerSettings = player,
				NGSettings = ng
			};
		}

		public async Task Save()
		{
			var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
			var local = await localFolder.CreateFileAsync("settings.json", CreationCollisionOption.OpenIfExists);

			var serializedText = JsonConvert.SerializeObject(this);

			await FileIO.WriteTextAsync(local, serializedText);
		}

		public AccountSettings AccontSettings { get; private set; }

		public RankingSettings RankingSettings { get; private set; }

		public PlayerSettings PlayerSettings { get; private set; }

		public NGSettings NGSettings { get; private set; }

		public HohoemaUserSettings()
		{
		}
	}

	[DataContract]
	public abstract class SettingsBase : BindableBase
	{
		public SettingsBase()
		{
			_FileLock = new SemaphoreSlim(1, 1);
		}


		public string FileName { get; private set; }

		public SemaphoreSlim _FileLock;

		public static async Task<T> Load<T>(string filename)
			where T : SettingsBase, new()
		{
			var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
			var local = await localFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

			var rawText = await FileIO.ReadTextAsync(local);
			if (!String.IsNullOrEmpty(rawText))
			{
				try
				{
					var obj = JsonConvert.DeserializeObject<T>(rawText);
					obj.FileName = filename;
					return obj;
				}
				catch
				{
					await local.DeleteAsync();
				}
			}

			var newInstance = new T()
			{
				FileName = filename
			};

			newInstance.OnInitialize();

			return newInstance;
		}


		public async Task Save()
		{
			try
			{
				await _FileLock.WaitAsync().ConfigureAwait(false);

				var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
				var local = await localFolder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);

				var serializedText = JsonConvert.SerializeObject(this);

				await FileIO.WriteTextAsync(local, serializedText);
			}
			finally
			{
				_FileLock.Release();
			}
		}

		public virtual void OnInitialize() { }

		protected virtual void Validate() { }
	}


	[DataContract]
	public class RankingSettings : SettingsBase
	{
		public RankingSettings()
			: base()
		{
			HighPriorityCategory = new ObservableCollection<RankingCategoryInfo>();
			MiddlePriorityCategory = new ObservableCollection<RankingCategoryInfo>();
			LowPriorityCategory = new ObservableCollection<RankingCategoryInfo>();
		}


		public void ResetCategoryPriority()
		{
			var highPrioUserRanking = HighPriorityCategory.Where(x => x.RankingSource == RankingSource.SearchWithMostPopular).ToList();
			var midPrioUserRanking = MiddlePriorityCategory.Where(x => x.RankingSource == RankingSource.SearchWithMostPopular).ToList();
			var lowPrioUserRanking = LowPriorityCategory.Where(x => x.RankingSource == RankingSource.SearchWithMostPopular).ToList();

			HighPriorityCategory.Clear();
			MiddlePriorityCategory.Clear();
			LowPriorityCategory.Clear();

			foreach (var info in highPrioUserRanking)
			{
				HighPriorityCategory.Add(info);
			}
			foreach (var info in midPrioUserRanking)
			{
				MiddlePriorityCategory.Add(info);
			}
			foreach (var info in lowPrioUserRanking)
			{
				LowPriorityCategory.Add(info);
			}

			var types = (IEnumerable<RankingCategory>)Enum.GetValues(typeof(RankingCategory));
			foreach (var type in types)
			{
				MiddlePriorityCategory.Add(RankingCategoryInfo.CreateFromRankingCategory(type));
			}
		}


		public override void OnInitialize()
		{
			ResetCategoryPriority();
		}

		protected override void Validate()
		{
			
		}

		
		


		private RankingTarget _Target;

		[DataMember]
		public RankingTarget Target
		{
			get
			{
				return _Target;
			}
			set
			{
				SetProperty(ref _Target, value);
			}
		}


		private RankingTimeSpan _TimeSpan;

		[DataMember]
		public RankingTimeSpan TimeSpan
		{
			get
			{
				return _TimeSpan;
			}
			set
			{
				SetProperty(ref _TimeSpan, value);
			}
		}




		private RankingCategory _Category;

		[DataMember]
		public RankingCategory Category
		{
			get
			{
				return _Category;
			}
			set
			{
				SetProperty(ref _Category, value);
			}
		}

		[DataMember]
		public ObservableCollection<RankingCategoryInfo> HighPriorityCategory { get; private set; }

		[DataMember]
		public ObservableCollection<RankingCategoryInfo> MiddlePriorityCategory { get; private set; }

		[DataMember]
		public ObservableCollection<RankingCategoryInfo> LowPriorityCategory { get; private set; }
		

		
	}


	public enum RankingSource
	{
		CategoryRanking,
		SearchWithMostPopular
	}



	public class RankingCategoryInfo : BindableBase, IEquatable<RankingCategoryInfo>
	{

		public static RankingCategoryInfo CreateFromRankingCategory(RankingCategory cat)
		{
			return new RankingCategoryInfo()
			{
				RankingSource = RankingSource.CategoryRanking,
				Parameter = cat.ToString(),
				DisplayLabel = cat.ToCultulizedText()
			};
		}


		public static RankingCategoryInfo CreateUserCustomizedRanking()
		{
			return new RankingCategoryInfo()
			{
				RankingSource = RankingSource.SearchWithMostPopular,
				Parameter = "",
				DisplayLabel = ""
			};
		}

		public RankingCategoryInfo()
		{
		}

		public string ToParameterString()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this);
		}

		public static RankingCategoryInfo FromParameterString(string json)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<RankingCategoryInfo>(json);
		}


		public RankingSource RankingSource { get; set; }


		private string _Parameter;
		public string Parameter
		{
			get { return _Parameter; }
			set { SetProperty(ref _Parameter, value); }
		}

		private string _DisplayLabel;
		public string DisplayLabel
		{
			get { return _DisplayLabel; }
			set { SetProperty(ref _DisplayLabel, value); }
		}
		
		public bool Equals(RankingCategoryInfo other)
		{
			if (other == null) { return false; }

			return this.RankingSource == other.RankingSource
				&& this.Parameter == other.Parameter;
		}
	}

	


	[DataContract]
	public class AccountSettings : SettingsBase
	{
		public AccountSettings()
			: base()
		{
			MailOrTelephone = "";
			Password = "";
		}



		private string _MailOrTelephone;

		[DataMember]
		public string MailOrTelephone
		{
			get
			{
				return _MailOrTelephone;
			}
			set
			{
				SetProperty(ref _MailOrTelephone, value);
			}
		}


		public bool IsValidMailOreTelephone
		{
			get
			{
				return !String.IsNullOrWhiteSpace(MailOrTelephone);
			}
		}

		

		private string _Password;

		[DataMember]
		public string Password
		{
			get
			{
				return _Password;
			}
			set
			{
				SetProperty(ref _Password, value);
			}
		}


		public bool IsValidPassword
		{
			get
			{
				return !String.IsNullOrWhiteSpace(Password);
			}
		}


		private bool _AutoLoginEnable;

		[DataMember]
		public bool AutoLoginEnable
		{
			get
			{
				return _AutoLoginEnable;
			}
			set
			{
				SetProperty(ref _AutoLoginEnable, value);
			}
		}

	}


	[DataContract]
	public class PlayerSettings : SettingsBase
	{
		public PlayerSettings()
			: base()
		{
			SoundVolume = 0.25f;
			DefaultCommentDisplay = true;
			IncrementReadablityOwnerComment = true;
			CommentRenderingFPS = 24;
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



		private uint _CommentRenderingFPS;


		[DataMember]
		public uint CommentRenderingFPS
		{
			get { return _CommentRenderingFPS; }
			set { SetProperty(ref _CommentRenderingFPS, value); }
		}
	}



	[DataContract]
	public class NGSettings : SettingsBase
	{

		public NGSettings()
		{
			NGVideoIdEnable = true;
			NGVideoIds = new ObservableCollection<VideoIdInfo>();
			NGVideoOwnerUserIdEnable = true;
			NGVideoOwnerUserIds = new ObservableCollection<UserIdInfo>();
			NGVideoTitleKeywordEnable = true;
			NGVideoTitleKeywords = new ObservableCollection<NGKeyword>();

			NGCommentUserIdEnable = true;
			NGCommentUserIds = new ObservableCollection<UserIdInfo>();
			NGCommentKeywordEnable = true;
			NGCommentKeywords = new ObservableCollection<NGKeyword>();
			NGCommentGlassMowerEnable = false;
			NGCommentScoreType = NGCommentScore.Middle;
		}


		public NGResult IsNgVideo(ThumbnailResponse res)
		{
			NGResult result = null;

			result = IsNgVideoOwnerId(res.UserId.ToString());
			if (result != null) return result;

			result = IsNGVideoId(res.Id);
			if (result != null) return result;

			result = IsNGVideoTitle(res.Title);
			if (result != null) return result;

			return result;
		}


		public NGResult IsNgVideoOwnerId(string userId)
		{

			if (this.NGVideoOwnerUserIdEnable && this.NGVideoOwnerUserIds.Count > 0)
			{
				var ngItem = this.NGVideoOwnerUserIds.SingleOrDefault(x => x.UserId == userId);

				if (ngItem != null)
				{
					return new NGResult()
					{
						NGReason = NGReason.UserId,
						Content = ngItem.UserId.ToString(),
						NGDescription = ngItem.Description
					};
				}
			}

			return null;
		}

		public NGResult IsNGVideoId(string videoId)
		{
			if (this.NGVideoIdEnable && this.NGVideoIds.Count > 0)
			{
				var ngItem = this.NGVideoIds.SingleOrDefault(x => x.VideoId == videoId);

				if (ngItem != null)
				{
					return new NGResult()
					{
						NGReason = NGReason.VideoId,
						Content = ngItem.VideoId,
						NGDescription = ngItem.Description,
					};
				}
			}
			return null;
		}

		public NGResult IsNGVideoTitle(string title)
		{
			if (this.NGVideoTitleKeywordEnable && this.NGVideoTitleKeywords.Count > 0)
			{
				var ngItem = this.NGVideoTitleKeywords.FirstOrDefault(x => title.Contains(x.Keyword));

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
				var ngItem = this.NGCommentKeywords.FirstOrDefault(x => commentText.Contains(x.Keyword));

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


		#region Video NG


		private bool _NGVideoIdEnable;

		[DataMember]
		public bool NGVideoIdEnable
		{
			get { return _NGVideoIdEnable; }
			set { SetProperty(ref _NGVideoIdEnable, value); }
		}


		[DataMember]
		public ObservableCollection<VideoIdInfo> NGVideoIds { get; private set; }


		private bool _NGVideoOwnerUserIdEnable;

		[DataMember]
		public bool NGVideoOwnerUserIdEnable
		{
			get { return _NGVideoOwnerUserIdEnable; }
			set { SetProperty(ref _NGVideoOwnerUserIdEnable, value); }
		}


		[DataMember]
		public ObservableCollection<UserIdInfo> NGVideoOwnerUserIds { get; private set; }


		private bool _NGVideoTitleKeywordEnable;

		[DataMember]
		public bool NGVideoTitleKeywordEnable
		{
			get { return _NGVideoTitleKeywordEnable; }
			set { SetProperty(ref _NGVideoTitleKeywordEnable, value); }
		}


		[DataMember]
		public ObservableCollection<NGKeyword> NGVideoTitleKeywords { get; private set; }

		#endregion


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


		private bool _NGCommentGlassMowerEnable;

		[DataMember]
		public bool NGCommentGlassMowerEnable
		{
			get { return _NGCommentGlassMowerEnable; }
			set { SetProperty(ref _NGCommentGlassMowerEnable, value); }
		}


		private NGCommentScore _NGCommentScoreType;

		[DataMember]
		public NGCommentScore NGCommentScoreType
		{
			get { return _NGCommentScoreType; }
			set { SetProperty(ref _NGCommentScoreType, value); }
		}

		#endregion


	}

	public class NGKeyword
	{
		public string TestText { get; set; }
		public string Keyword { get; set; }
	}

	public class VideoIdInfo
	{
		public string VideoId { get; set; }
		public string Description { get; set; }
	}

	public class UserIdInfo
	{
		public string UserId { get; set; }
		public string Description { get; set; }
	}

	public enum NGCommentScore
	{
		None,
		Low,
		Middle,
		High,
		VeryHigh,
		SuperVeryHigh,
		UltraSuperVeryHigh
	}


	public static class NGCommentScoreHelper
	{
		public static int GetCommentScoreAmount(this NGCommentScore scoreType)
		{
			switch (scoreType)
			{
				case NGCommentScore.None:
					return int.MinValue;
				case NGCommentScore.Low:
					return -10000;
				case NGCommentScore.Middle:
					return -7200;
				case NGCommentScore.High:
					return -4800;
				case NGCommentScore.VeryHigh:
					return -2400;
				case NGCommentScore.SuperVeryHigh:
					return -600;
				case NGCommentScore.UltraSuperVeryHigh:
					return 0;
				default:
					throw new NotSupportedException();
			}
		}
	}
}
