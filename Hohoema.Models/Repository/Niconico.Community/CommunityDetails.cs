using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Community
{

	public class CommunityDetail
	{
        private Mntone.Nico2.Communities.Detail.CommunityDetail _res;

		public bool IsOk { get; }

        public CommunityDetail(bool isOK, Mntone.Nico2.Communities.Detail.CommunityDetail res)
        {
            _res = res;
			NewsList = res.NewsList.Select(x => new CommunityNews() 
			{
				Title = x.Title,
				PostDate = x.PostDate,
				PostAuthor = x.PostAuthor,
				ContentHtml = x.ContentHtml,
			}).ToList();

			CurrentLiveList = res.CurrentLiveList.Select(x => new CommunityLiveInfo() 
			{
				LiveId = x.LiveId,
				LiveTitle = x.LiveTitle,
			}).ToList();

			RecentLiveList = res.RecentLiveList.Select(x => new LiveInfo() 
			{
				LiveId = x.LiveId,
				Title = x.Title,
				StartTime = x.StartTime,
				StreamerName = x.StreamerName,
			}).ToList();

			FutureLiveList = res.FutureLiveList.Select(x => new LiveInfo() 
			{
				LiveId = x.LiveId,
				Title = x.Title,
				StartTime = x.StartTime,
				StreamerName = x.StreamerName,
			}).ToList();

			VideoList = res.VideoList.Select(x => new CommunityVideo() 
			{
				Title = x.Title,
				VideoId = x.VideoId,
				ThumbnailUrl = x.ThumbnailUrl,
			}).ToList();

			SampleFollwers = res.SampleFollwers.Select(x => new CommunityMember() 
			{
				Name = x.Name,
				UserId = x.UserId,
				IconUrl = x.IconUrl
			}).ToList();
        }

		public string Name => _res.Name;
		public string Id => _res.Id;
		public Uri IconUrl => _res.IconUrl;
		//public DateTime DateTime => _res.DateTime
		public string ShortDescription => _res.ShortDescription;
		public uint Level => _res.Level;
		public uint MemberCount => _res.MemberCount;
		public uint VideoCount => _res.VideoCount;

		// オーナー
		public string OwnerUserName => _res.OwnerUserName;
		public string OwnerUserId => _res.OwnerUserId;

		public uint FollowerMaxCount => _res.FollowerMaxCount;

		public uint VideoMaxCount => _res.VideoCount;

		public string ProfielHtml => _res.ProfielHtml;

		public List<string> Tags => _res.Tags;


		public List<CommunityNews> NewsList { get; }

		public List<CommunityLiveInfo> CurrentLiveList { get; }


		public List<LiveInfo> RecentLiveList { get; }

		public List<LiveInfo> FutureLiveList { get; }

		public List<CommunityVideo> VideoList { get; }

		public List<CommunityMember> SampleFollwers { get; }

		// public CommunityOption Option { get; private set; } = new CommunityOption();

		public string PrivilegeDescription => _res.PrivilegeDescription;
	}

	public class CommunityLiveInfo
	{
		public string LiveTitle { get; set; }

		public string LiveId { get; set; }
	}

	public class CommunityNews
	{
		public string Title { get; set; }
		public string ContentHtml { get; set; }
		public DateTime PostDate { get; set; }
		public string PostAuthor { get; set; }
	}

	public class CommunityOption
	{
		/// <summary>
		/// 登録申請を自動で承認
		/// </summary>
		public bool IsJoinAutoAccept { get; set; }

		/// <summary>
		/// 登録時に個人情報公開不要
		/// </summary>
		public bool IsJoinWithoutPrivacyInfo { get; set; }

		/// <summary>
		/// 新参メンバー動画投稿可
		/// </summary>
		public bool IsCanSubmitVideoOnlyPrivilege { get; set; }

		/// <summary>
		/// 新参メンバー登録承認可
		/// </summary>
		public bool IsCanAcceptJoinOnlyPrivilege { get; set; }

		/// <summary>
		/// 特権メンバーのみ生放送可
		/// </summary>
		public bool IsCanLiveOnlyPrivilege { get; set; }
	}


	public class LiveInfo
	{
		public DateTime StartTime { get; set; }
		public string Title { get; set; }
		public string LiveId { get; set; }
		public string StreamerName { get; set; }
	}


	public class CommunityMember
	{
		public uint UserId { get; set; }
		public string Name { get; set; }
		public Uri IconUrl { get; set; }
	}

	public class CommunityVideo
	{
		public string Title { get; set; }
		public string VideoId { get; set; }
		public string ThumbnailUrl { get; set; }
	}

}
