using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Models.Db;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	[DataContract]
	public class NGSettings : SettingsBase
	{

		public NGSettings()
		{
			NGVideoIdEnable = true;
			NGVideoIds = new ObservableCollection<VideoIdInfo>();
			NGVideoOwnerUserIdEnable = true;
			NGVideoOwnerUserIds = new ObservableCollection<UserIdInfo>();
			NGVideoTitleKeywordEnable = false;
			NGVideoTitleKeywords = new ObservableCollection<NGKeyword>();

			NGCommentUserIdEnable = true;
			NGCommentUserIds = new ObservableCollection<UserIdInfo>();
			NGCommentKeywordEnable = true;
			NGCommentKeywords = new ObservableCollection<NGKeyword>();
			NGCommentScoreType = NGCommentScore.Middle;
		}


		public NGResult IsNgVideo(Database.NicoVideo info)
		{
			NGResult result = null;

            result = IsNgVideoOwnerId(info.Owner.OwnerId);
            if (result != null) return result;

            result = IsNGVideoTitle(info.Title);
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
            if (string.IsNullOrEmpty(title)) { return null; }

			if (this.NGVideoTitleKeywordEnable && this.NGVideoTitleKeywords.Count > 0)
			{
                var ngItem = this.NGVideoTitleKeywords.FirstOrDefault(x => x.CheckNG(title));

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


		

		private NGCommentScore _NGCommentScoreType;

		[DataMember]
		public NGCommentScore NGCommentScoreType
		{
			get { return _NGCommentScoreType; }
			set { SetProperty(ref _NGCommentScoreType, value); }
		}

		#endregion






		public void AddNGVideoOwnerId(string userId, string userName)
		{
			RemoveNGVideoOwnerId(userId);

			var userIdInfo = new UserIdInfo()
			{
				UserId = userId,
				Description = userName
			};

			NGVideoOwnerUserIds.Add(userIdInfo);

            Save().ConfigureAwait(false);
		}

		public bool RemoveNGVideoOwnerId(string userId)
		{
            try
            {
                var item = NGVideoOwnerUserIds.SingleOrDefault(x => x.UserId == userId);
                if (item != null)
                {
                    return NGVideoOwnerUserIds.Remove(item);
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                Save().ConfigureAwait(false);
            }
		}

	}

    [DataContract]
	public class NGKeyword
	{
        [DataMember]
		public string TestText { get; set; }


        private string _Keyword;
        [DataMember]
        public string Keyword
        {
            get { return _Keyword; }
            set
            {
                if (_Keyword != value)
                {
                    _Keyword = value;
                    _Regex = null;
                }
            }
        }


        private Regex _Regex;

        public bool CheckNG(string target)
        {
            if (_Regex == null)
            {
                try
                {
                    _Regex = new Regex(Keyword);
                }
                catch { }
            }

            return _Regex?.IsMatch(target) ?? false;
        }
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
