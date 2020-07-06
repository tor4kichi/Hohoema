using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.App
{
    public sealed class VideoListFilterSettings : FlagsRepositoryBase
    {
		NgVideoRepository _ngVideoRepository;
		NgVideoOwnerRepository _ngVideoOwnerRepository;
		NgVideoTitleKeywordRepository _ngVideoTitleKeywordRepository;

		public VideoListFilterSettings(
			NgVideoRepository ngVideoRepository,
			NgVideoOwnerRepository ngVideoOwnerRepository,
			NgVideoTitleKeywordRepository ngVideoTitleKeywordRepository
			)
        {
			_NGVideoOwnerUserIdEnable = Read(true, nameof(NGVideoOwnerUserIdEnable));
			_NGVideoIdEnable = Read(true, nameof(NGVideoIdEnable));
			_NGVideoTitleKeywordEnable = Read(true, nameof(NGVideoTitleKeywordEnable));

			_ngVideoRepository = ngVideoRepository;
            _ngVideoOwnerRepository = ngVideoOwnerRepository;
            _ngVideoTitleKeywordRepository = ngVideoTitleKeywordRepository;

			_NgVideoMap = _ngVideoRepository.ReadAllItems().ToDictionary(x => x.VideoId);
		}

		private bool _NGVideoIdEnable;
		public bool NGVideoIdEnable
		{
			get { return _NGVideoIdEnable; }
			set { SetProperty(ref _NGVideoIdEnable, value); }
		}

		
		public bool IsNgVideo(IVideoContent info, out NGResult result)
		{
			if (IsNGVideo(info.Id, out result))
            {
				return true;
            }
			else if (IsNgVideoOwner(info.ProviderId, out result))
            {
				return true;
            }
			else if (IsNgVideoTitle(info.Label, out result))
            {
				return true;
            }
			else
            {
				return false;
            }
		}



		public bool IsNGVideo(string videoId, out NGResult result)
		{
			if (this.NGVideoIdEnable && _NgVideoMap.Any())
			{
				if (_NgVideoMap.TryGetValue(videoId, out var ngItem))
				{
					result = new NGResult()
					{
						NGReason = NGReason.VideoId,
						Content = ngItem.VideoId,
						NGDescription = ngItem.Description,
					};
					return true;
				}
			}
			result =  null;
			return false;
		}


		public bool IsNgVideoOwner(string userId, out NGResult result)
		{
			if (userId == null) 
			{
				result = null;
				return false; 
			}

			if (this.NGVideoOwnerUserIdEnable)
			{
				var ngItem = _ngVideoOwnerRepository.Get(userId);

				if (ngItem != null)
				{
					result = new NGResult()
					{
						NGReason = NGReason.UserId,
						Content = ngItem.UserId.ToString(),
						NGDescription = ngItem.Description
					};
					return true;
				}
			}

			result = null;
			return false;
		}

		public bool IsNgVideoTitle(string title, out NGResult result)
		{
			if (string.IsNullOrEmpty(title)) 
			{
				result = null;
				return false; 
			}


			
			if (this.NGVideoTitleKeywordEnable)
			{
				_ngVideoTitleKeywords ??= GetAllNGVideoTitleKeyword();
				var ngKeyword = _ngVideoTitleKeywords.Find(x => x.CheckNG(title));
				if (ngKeyword != null)
				{
					result = new NGResult()
					{
						NGReason = NGReason.Keyword,
						Content = ngKeyword.Keyword,
					};

					return true;
				}
			}

			result = null;
			return false;
		}





		public class NgVideoRepository : LiteDBServiceBase<VideoIdInfo>
        {
			public NgVideoRepository(ILiteDatabase liteDatabase)
				: base(liteDatabase)
			{
				_collection.EnsureIndex(x => x.VideoId);
            }

			public bool IsNgVideo(string videoId)
            {
				return _collection.Exists(x => x.VideoId == videoId);
            }
        }

		Dictionary<string, VideoIdInfo> _NgVideoMap;

		public List<VideoIdInfo> GetAllNGVideos()
        {
			return _ngVideoRepository.ReadAllItems();
		}

		public VideoIdInfo AddNGVideo(string videoId, string videoTitle)
        {
			var item = _ngVideoRepository.CreateItem(new VideoIdInfo() { VideoId = videoId, Description = videoTitle });
			_NgVideoMap.TryAdd(item.VideoId, item);
			return item;
		}

		public bool RemoveNgVideo(string videoId)
        {
			_NgVideoMap.Remove(videoId);
			return _ngVideoRepository.DeleteItem(videoId);
		}


		public bool IsNgVideoId(string videoId)
		{
			return _ngVideoRepository.IsNgVideo(videoId);
		}



		private bool _NGVideoOwnerUserIdEnable;
		public bool NGVideoOwnerUserIdEnable
		{
			get { return _NGVideoOwnerUserIdEnable; }
			set { SetProperty(ref _NGVideoOwnerUserIdEnable, value); }
		}


		public class NgVideoOwnerRepository : LiteDBServiceBase<UserIdInfo>
		{
			public NgVideoOwnerRepository(ILiteDatabase liteDatabase) 
				: base(liteDatabase)
            {
				_collection.EnsureIndex(x => x.UserId);
            }

			public UserIdInfo Get(string userId)
            {
				return _collection.FindById(userId);
            }
		}


		public bool IsNgVideoOwner(string userId)
        {
			return _ngVideoOwnerRepository.Exists(x => x.UserId == userId);
        }


		public List<UserIdInfo> GetAllNGVideoOwner()
		{
			return _ngVideoOwnerRepository.ReadAllItems();
		}

		public UserIdInfo AddNgVideoOwner(string userId, string userName)
		{
			return _ngVideoOwnerRepository.CreateItem(new UserIdInfo() { UserId = userId, Description = userName });
		}

		public bool RemoveNgVideoOwner(string userId)
		{
			return _ngVideoOwnerRepository.DeleteItem(userId);
		}


		private bool _NGVideoTitleKeywordEnable;
		public bool NGVideoTitleKeywordEnable
		{
			get { return _NGVideoTitleKeywordEnable; }
			set { SetProperty(ref _NGVideoTitleKeywordEnable, value); }
		}


		List<NGKeyword> _ngVideoTitleKeywords;


		
		public sealed class NgVideoTitleKeywordRepository : LiteDBServiceBase<NGKeyword>
		{
			public NgVideoTitleKeywordRepository(ILiteDatabase liteDatabase)
			: base(liteDatabase)
			{ }
		}

		public bool IsNgVideoTitle(string title)
        {
			_ngVideoTitleKeywords ??= GetAllNGVideoTitleKeyword();

			return _ngVideoTitleKeywords.Any(x => x.CheckNG(title));
		}

		public List<NGKeyword> GetAllNGVideoTitleKeyword()
		{
			return _ngVideoTitleKeywords = _ngVideoTitleKeywordRepository.ReadAllItems();
		}

		public NGKeyword AddNGVideoTitleKeyword(string keyword, string testText)
		{
			_ngVideoTitleKeywords = null;
			return _ngVideoTitleKeywordRepository.CreateItem(new NGKeyword() { Keyword = keyword, TestText = testText });
		}

		public bool RemoveNgVideoTitleKeyword(NGKeyword keyword)
		{
			_ngVideoTitleKeywords = null;
			return _ngVideoTitleKeywordRepository.DeleteItem(keyword.Id);
		}

		public NGKeyword UpdateNgVideoTitleKeyword(NGKeyword keyword)
		{
			_ngVideoTitleKeywords = null;
			return _ngVideoTitleKeywordRepository.UpdateItem(keyword);
		}
	}

	public class NGKeyword
	{
		[BsonId(autoId:true)]
		public int Id { get; private set; }

		[BsonField]
		public string TestText { get; set; }


		private string _Keyword;
		[BsonField]
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
		[BsonId]
		public string VideoId { get; set; }
		[BsonField]
		public string Description { get; set; }
	}

	public class UserIdInfo
	{
		[BsonId]
		public string UserId { get; set; }

		[BsonField]
		public string Description { get; set; }
	}




	public class NGResult
	{
		public NGReason NGReason { get; set; }
		public string NGDescription { get; set; } = "";
		public string Content { get; set; }
	}

	public enum NGReason
	{
		VideoId,
		UserId,
		Keyword,
		Tag,
	}
}
