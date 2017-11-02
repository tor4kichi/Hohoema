using NicoPlayerHohoema.Helpers;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Foundation;
using Windows.UI.Core;
using System.Collections.ObjectModel;

namespace NicoPlayerHohoema.Models
{
    public class FeedGroupLogic : BindableBase, IFeedGroup
    {
        static public async Task<FeedGroupLogic> Load(FeedGroup2 source, IFileAccessor<List<FeedItem>> itemsFileAccessor)
        {
            try
            {
                var items = await itemsFileAccessor.Load();
                var feedGroup = new FeedGroupLogic(source, items);
                feedGroup._FeedItemsFileAccessor = itemsFileAccessor;

                return feedGroup;
            }
            catch
            {
                Debug.WriteLine("");
                return null;
            }
        }


        public const int MaxFeedItemsCount = 100;

        public HohoemaApp HohoemaApp { get; internal set; }
        public FeedManager FeedManager { get; internal set; }

        AsyncLock _UpdateLock = new AsyncLock();

        public Guid Id => Source.Id;

        private string _Label;
        public string Label
        {
            get { return _Label; }
            set
            {
                if (SetProperty(ref _Label, value))
                {
                    Source.Label = _Label;
                }
            }
        }

        public DateTime UpdateTime => Source.UpdateTime;

        private DateTime _SourceModifiedTime;
        public DateTime SourceModifiedTime
        {
            get { return _SourceModifiedTime; }
            set
            {
                if (SetProperty(ref _SourceModifiedTime, value))
                {
                    RaisePropertyChanged(nameof(IsRefreshRequired));
                }
            }
        }

        public bool IsRefreshRequired { get; private set; }


        IFileAccessor<List<FeedItem>> _FeedItemsFileAccessor;
        ObservableCollection<FeedItem> _FeedItems { get; }
        ReadOnlyObservableCollection<FeedItem> Items { get; }
        public IList<FeedItem> FeedItems => Items;

        private ObservableCollection<IFeedSource> _FeedSourceList { get; }
        public IReadOnlyList<IFeedSource> FeedSourceList => _FeedSourceList;


        FeedGroup2 Source { get; }


        public FeedGroupLogic(FeedGroup2 source, IList<FeedItem> items = null)
        {
            Source = source;
            _FeedItems = new ObservableCollection<FeedItem>(items ?? new List<FeedItem>());
            Items = new ReadOnlyObservableCollection<FeedItem>(_FeedItems);
            _FeedSourceList = new ObservableCollection<IFeedSource>(source.FeedSourceList);
        }



        public IFeedSource AddTagFeedSource(string tag)
        {
            if (ExistFeedSource(FollowItemType.Tag, tag)) { return null; }

            var feedSource = new TagFeedSource(tag);
            AddFeedSource(feedSource);

            return feedSource;
        }

        public IFeedSource AddMylistFeedSource(string name, string mylistGroupId)
        {
            if (ExistFeedSource(FollowItemType.Mylist, mylistGroupId)) { return null; }

            var feedSource = new MylistFeedSource(name, mylistGroupId);
            AddFeedSource(feedSource);

            return feedSource;
        }

        public IFeedSource AddUserFeedSource(string name, string userId)
        {
            if (ExistFeedSource(FollowItemType.User, userId)) { return null; }

            var feedSource = new UserFeedSource(name, userId);

            AddFeedSource(feedSource);

            return feedSource;
        }


        private void AddFeedSource(IFeedSource feedSource)
        {
            _FeedSourceList.Add(feedSource);
            Source.FeedSourceList.Add(feedSource);
            SourceModifiedTime = DateTime.Now;
            FeedManager.SaveOne(this);
        }

        public void RemoveUserFeedSource(IFeedSource feedSource)
        {
            if (Source.FeedSourceList.Remove(feedSource))
            {
                _FeedSourceList.Remove(feedSource);
                SourceModifiedTime = DateTime.Now;
                FeedManager.SaveOne(this);
            }
        }


        public bool ExistFeedSource(FollowItemType itemType, string id)
        {
            return _FeedSourceList.Any(x => x.FollowItemType == itemType && x.Id == id);
        }

        public async Task Refresh()
        {
            await HohoemaApp.UIDispatcher.RunAsync(
                CoreDispatcherPriority.Normal, 
                async () => 
            {
                await _Refresh();
            });
        }

        private async Task _Refresh()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (!IsRefreshRequired) { return; }

                Debug.WriteLine($"{Label} starting update feed.");

                var updateTime = DateTime.Now;
                var latestItems = new List<FeedItem>();
                foreach (var feedSource in _FeedSourceList)
                {
                    var items = await feedSource.GetLatestItems(HohoemaApp);
                    foreach (var item in items)
                    {
                        latestItems.Add(item);
                    }
                }


                var latestOrderedItems = latestItems
                    .OrderByDescending(x => x.SubmitDate)
                    .Take(MaxFeedItemsCount)
                    .ToList();

                foreach (var item in latestOrderedItems)
                {
                    item.CheckedTime = updateTime;
                }

                var exceptItems = latestOrderedItems.Except(FeedItems, FeedItemComparer.Default).ToList();

                var addedItems = exceptItems.Where(x => x.CheckedTime == updateTime);

                var removedItems = FeedItems
                    .Except(latestOrderedItems, FeedItemComparer.Default)
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
                            var nicoVideo = HohoemaApp.MediaManager.GetNicoVideo(addItem.VideoId);

                            addItem.SubmitDate = nicoVideo.PostedAt;
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

                Source.UpdateTime = updateTime;
                RaisePropertyChanged();

                await FeedManager.SaveOne(this);

                Debug.WriteLine($"{Label} update feed done.");
            }
            
        }

        public bool MarkAsRead(string videoId)
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


        public async Task<bool> Rename(string newLabel)
        {
            if (await FeedManager.RenameFeedGroup(this, newLabel))
            {
                Source.Label = newLabel;
                RaisePropertyChanged(nameof(Label));

                await FeedManager.SaveOne(this);

                return true;
            }
            else
            {
                return false;
            }
        }


        internal Task Teardown()
        {
            return _FeedItemsFileAccessor.Delete();
        }
    }



    // ユーザーが指定したFavItemを束ねて、動画Feedを生成する
    [DataContract]
	[KnownType(typeof(TagFeedSource))]
	[KnownType(typeof(MylistFeedSource))]
	[KnownType(typeof(UserFeedSource))]
	public class FeedGroup2 
	{
		#region Properties

		[DataMember(Name = "id")]
        public Guid Id { get; set; }

		[DataMember(Name = "label")]
		public string Label { get; set; }
        


		[DataMember(Name = "feed_source_list")]
		private List<IFeedSource> _FeedSourceList;


		public IList<IFeedSource> FeedSourceList
		{
			get
			{
				return _FeedSourceList;
			}
		}

		[DataMember(Name = "update_time")]
        public DateTime UpdateTime { get; set; }


		#endregion

        public FeedGroup2()
		{
			Id = Guid.NewGuid();
			Label = "";
			_FeedSourceList = new List<IFeedSource>();
		}

		public FeedGroup2(string label)
		{
			Id = Guid.NewGuid();
			Label = label;
			_FeedSourceList = new List<IFeedSource>();
		}

		public FeedGroup2(FeedGroup legacy)
		{
			Id = legacy.Id;
			Label = legacy.Label;
			_FeedSourceList = legacy.FeedSourceList.ToList();
			UpdateTime = legacy.UpdateTime;
		}


		
		
	}

	[DataContract]
	[KnownType(typeof(TagFeedSource))]
	[KnownType(typeof(MylistFeedSource))]
	[KnownType(typeof(UserFeedSource))]
	public class FeedGroup : BindableBase
	{
		public const int MaxFeedItemsCount = 50;

		#region Properties

		public HohoemaApp HohoemaApp { get; internal set; }
		public FeedManager FeedManager { get; internal set; }

		[DataMember(Name = "id")]
		private Guid _Id;
		public Guid Id
		{
			get { return _Id; }
			private set { SetProperty(ref _Id, value); }
		}

		[DataMember(Name = "label")]
		private string _Label;
		public string Label
		{
			get { return _Label; }
			internal set { SetProperty(ref _Label, value); }
		}


		[DataMember(Name = "feed_source_list")]
		private List<IFeedSource> _FeedSourceList = new List<IFeedSource>();


		public IReadOnlyList<IFeedSource> FeedSourceList
		{
			get
			{
				return _FeedSourceList;
			}
		}



		[DataMember(Name = "feed_items")]
		public List<FeedItem> FeedItems { get; private set; }

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


		

	}
}
