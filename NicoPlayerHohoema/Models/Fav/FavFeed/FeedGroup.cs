using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	// ユーザーが指定したFavItemを束ねて、動画Feedを生成する
	[DataContract]
	[KnownType(typeof(TagFeedSource))]
	[KnownType(typeof(MylistFeedSource))]
	[KnownType(typeof(UserFeedSource))]
	public class FeedGroup : BindableBase
	{


		#region Properties

		public HohoemaApp HohoemaApp { get; internal set; }
		public FeedManager FeedManager { get; internal set; }


		[DataMember(Name = "label")]
		private string _Label;
		public string Label
		{
			get { return _Label; }
			internal set { SetProperty(ref _Label, value); }
		}


		[DataMember(Name = "feed_source_list")]
		private List<IFeedSource> _FeedSourceList;


		public IReadOnlyList<IFeedSource> FeedSourceList
		{
			get
			{
				return _FeedSourceList;
			}
		}



		[DataMember(Name = "feed_items")]
		public List<FavFeedItem> FeedItems { get; private set; }

		[DataMember(Name = "update_time")]
		private DateTime _UpdateTime;
		public DateTime UpdateTime
		{
			get { return _UpdateTime; }
			private set { SetProperty(ref _UpdateTime, value); }
		}

		[DataMember(Name = "is_need_refresh")]
		private bool _IsNeedRefresh;
		public bool IsNeedRefresh
		{
			get { return _IsNeedRefresh; }
			internal set { SetProperty(ref _IsNeedRefresh, value); }
		}


		#endregion






		public FeedGroup(string label)
		{
			Label = label;
			_FeedSourceList = new List<IFeedSource>();
			FeedItems = new List<FavFeedItem>();
			IsNeedRefresh = false;
		}


		public IFeedSource AddTagFeedSource(string tag)
		{
			if (ExistFeedSource(FavoriteItemType.Tag, tag)) { return null; }

			var feedSource = new TagFeedSource(tag);
			_FeedSourceList.Add(feedSource);

			IsNeedRefresh = true;

			return feedSource;
		}

		public IFeedSource AddMylistFeedSource(string name, string mylistGroupId)
		{
			if (ExistFeedSource(FavoriteItemType.Mylist, mylistGroupId)) { return null; }

			var feedSource = new MylistFeedSource(name, mylistGroupId);
			_FeedSourceList.Add(feedSource);

			IsNeedRefresh = true;

			return feedSource;
		}

		public IFeedSource AddUserFeedSource(string name, string userId)
		{
			if (ExistFeedSource(FavoriteItemType.User, userId)) { return null; }

			var feedSource = new UserFeedSource(name, userId);
			_FeedSourceList.Add(feedSource);

			IsNeedRefresh = true;

			return feedSource;
		}

		public void RemoveUserFeedSource(IFeedSource feedSource)
		{
			if (_FeedSourceList.Remove(feedSource))
			{
				IsNeedRefresh = true;
			}
		}


		public bool ExistFeedSource(FavoriteItemType itemType, string id)
		{
			return _FeedSourceList.Any(x => x.FavoriteItemType == itemType && x.Id == id);
		}


		public async Task Refresh()
		{
			Debug.WriteLine($"{Label} starting update feed.");

			var updateTime = DateTime.Now;
			var latestItems = new List<FavFeedItem>();
			foreach (var feedSource in _FeedSourceList)
			{
				var items = await feedSource.GetLatestItems(HohoemaApp);
				latestItems.AddRange(items);
			}


			var latestOrderedItems = latestItems
				.OrderBy(x => x.SubmitDate)
				.Take(100)
				.ToList();

			foreach (var item in latestOrderedItems)
			{
				item.CheckedTime = updateTime;
			}

			var exceptItems = latestOrderedItems.Except(FeedItems, FavFeedItemComparer.Default).ToList();

			var addedItems = exceptItems.Where(x => x.CheckedTime == updateTime);

			var removedItems = FeedItems
				.Except(latestOrderedItems, FavFeedItemComparer.Default)
				.Where(x => x.CheckedTime != updateTime)
				.ToList();


			foreach (var addItem in addedItems)
			{
				addItem.IsUnread = true;

				// 投稿日時が初期化されていない場合はThumbnailInfoから拾ってくる

				// ユーザー動画取得の場合に投稿時刻が取れないことが原因
				// 追加されたアイテムだけのThumbnailを取得することで無駄な処理を減らす
				if (addItem.SubmitDate == default(DateTime))
				{
					try
					{
						var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideo(addItem.VideoId);
						var thumbnail = await nicoVideo.GetThumbnailResponse();

						addItem.SubmitDate = thumbnail.PostedAt.DateTime;
					}
					catch (Exception ex)
					{
						Debug.Fail("UserFeedItem 更新中、NicoVideoオブジェクトの取得に失敗しました。", ex.Message);
					}
				}
				

				FeedItems.Add(addItem);
			}

			foreach (var removedItem in removedItems)
			{
				var item = FeedItems.SingleOrDefault(x => x.VideoId == removedItem.VideoId);
				if (item != null)
				{
					item.IsDeleted = true;
					FeedItems.Remove(item);
				}

			}

			FeedItems.Sort(FavFeedItemComparer.Default);

			UpdateTime = updateTime;

			await FeedManager.SaveOne(this);


			IsNeedRefresh = false;

			Debug.WriteLine($"{Label} update feed done.");
		}

		internal bool MarkAsRead(string videoId)
		{
			bool isChanged = false;
			foreach (var item in FeedItems)
			{
				if (item.IsUnread && item.VideoId == videoId)
				{
					item.IsUnread = false;
					isChanged = true;
				}
			}

			return isChanged;
		}

		public void ForceMarkAsRead()
		{
			foreach (var item in FeedItems)
			{
				item.IsUnread = false;
			}
		}

		public int GetUnreadItemCount()
		{
			return FeedItems.Count(x => x.IsUnread);
		}


		public Task Rename(string newLabel)
		{
			return FeedManager.RenameFeedGroup(this, newLabel);
		}


	}
}
