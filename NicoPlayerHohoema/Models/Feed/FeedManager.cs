using NicoPlayerHohoema.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Foundation;
using Windows.UI.Core;
using System.Threading;

namespace NicoPlayerHohoema.Models
{
    // FavFeedGroupを管理する

    // 常にFavManagerへのアクションを購読して
    // 自身が管理するfavFeedGroupが参照しているFavが購読解除された場合に、
    // グループ内からも削除するように働く

    // フィードの更新を指揮する

    // フィードの保存処理をコントロールする


    public delegate void FeedGroupAddedEventHanlder(Database.Feed feedGroup);
    public delegate void FeedGroupRemovedEventHanlder(Database.Feed feedGroup);


    public struct FeedSourceRemovedEventArgs
    {
        public Database.Feed Feed { get; set; }
        public Database.Bookmark Bookmark { get; set; }
    }


    public struct FeedUpdateEventArgs
    {
        public Database.Feed Feed { get; set; }
        public IList<Tuple<Database.NicoVideo, Database.Bookmark>> Items { get; set; }
    }

    public class FeedManager : AsyncInitialize
	{
        static Newtonsoft.Json.JsonSerializerSettings FeedGroupSerializerSettings = new Newtonsoft.Json.JsonSerializerSettings()
        {
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects
        };

        public static Database.BookmarkType FollowItemTypeConvertToFeedSourceType(FollowItemType followType)
        {
            switch (followType)
            {
                case FollowItemType.Tag:
                    return Database.BookmarkType.SearchWithTag;
                case FollowItemType.Mylist:
                    return Database.BookmarkType.Mylist;
                case FollowItemType.User:
                    return Database.BookmarkType.User;
                default:
                    throw new NotSupportedException(followType.ToString() + " is not support feed group source type.");
            }
        }


        public HohoemaApp HohoemaApp { get; private set; }

        public event FeedGroupAddedEventHanlder FeedGroupAdded;
        public event FeedGroupRemovedEventHanlder FeedGroupRemoved;
        public event EventHandler<FeedUpdateEventArgs> FeedUpdated;


        public event EventHandler<FeedSourceRemovedEventArgs> FeedSourceRemoved;



        public FeedManager(HohoemaApp hohoemaApp)
		{
			HohoemaApp = hohoemaApp;
		}

        public List<Database.Feed> GetAllFeedGroup()
        {
            return Database.FeedDb.GetAll();
        }

		public Task<StorageFolder> GetFeedGroupFolder()
		{
			return HohoemaApp.GetFeedSettingsFolder();
		}

        protected override Task OnInitializeAsync(CancellationToken token)
        {
            return HohoemaApp.UIDispatcher.RunIdleAsync(async (_) =>
            {
                // フィードのJSONファイルからLiteDBへの移行処理
                try
                {
                    await MigrateBefore_0_11();
                }
                catch
                {
                    // 正常動作に影響を与えぬよう例外を握りつぶす
                }
            })
            .AsTask();
        }

        /// <summary>
        /// 0.11.x以前のフィード情報をLiteDbへと移行します
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
		public async Task MigrateBefore_0_11()
		{
            var feedGroupFolder = await GetFeedGroupFolder();
            if (feedGroupFolder == null)
            {
                return;
            }

            var files = await feedGroupFolder.GetFilesAsync();

            foreach (var file in files)
			{
				if (file.FileType == ".json")
				{
					var fileName = file.Name;

                    try
                    {
                        var fileAccessor = new FolderBasedFileAccessor<FeedGroup2>(feedGroupFolder, fileName);
                        var item = await fileAccessor.Load(FeedGroupSerializerSettings);

						if (item != null)
						{
                            var newDbFeedGroup = new Database.Feed(item.Label);

                            if (item.FeedSourceList != null)
                            {
                                foreach (var source in item.FeedSourceList)
                                {
                                    newDbFeedGroup.AddSource(new Database.Bookmark()
                                    {
                                        Label = source.Name,
                                        Content = source.Id,
                                        BookmarkType = FollowItemTypeConvertToFeedSourceType(source.FollowItemType)
                                    });
                                }
                            }

                            Database.FeedDb.AddOrUpdate(newDbFeedGroup);

                            Debug.WriteLine($"FeedManager: [Sucesss] load {item.Label}");

                            continue;
                        }
                        else
						{
							Debug.WriteLine($"FeedManager: [?] .json but not FeedGroup file < {fileName}");
						}
					}
					catch
					{
						Debug.WriteLine($"FeedManager: [Failed] load {file.Path}");
					}
                }
            }

            await feedGroupFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
        }


        
		public Database.Feed GetFeedGroup(int id)
		{
            return Database.FeedDb.Get(id);
		}
		
		public Database.Feed AddFeedGroup(string label)
		{
            var feedGroup = new Database.Feed(label);

            Database.FeedDb.AddOrUpdate(feedGroup);

            FeedGroupAdded?.Invoke(feedGroup);

            return feedGroup;
		}

		public bool RemoveFeedGroup(Database.Feed feedGroup)
		{
            if (Database.FeedDb.Delete(feedGroup))
            {
                FeedGroupRemoved?.Invoke(feedGroup);
                return true;
            }
            else
            {
                return false;
            }
        }


		internal void UpdateFeedGroup(Database.Feed feedGroup)
		{
            Database.FeedDb.AddOrUpdate(feedGroup);
        }

        public async Task<List<Tuple<Database.NicoVideo, Database.Bookmark>>> RefreshFeedItemsAsync(Database.Feed feedGroup)
        {
            var items = feedGroup.Sources;

            List<Tuple<Database.NicoVideo, Database.Bookmark>> itemsContainer = new List<Tuple<Database.NicoVideo, Database.Bookmark>>();
            foreach (var source in items)
            {
                var feedItems = await GetItems(source);
                itemsContainer.AddRange(feedItems.Select(x => new Tuple<Database.NicoVideo, Database.Bookmark>(x, source)));
            }

            feedGroup.UpdateAt = DateTime.Now;

            UpdateFeedGroup(feedGroup);

            var finalItemsList = itemsContainer
                .OrderByDescending(x => x.Item1.PostedAt)
                .ToList();

            FeedUpdated?.Invoke(this, new FeedUpdateEventArgs()
            {
                Feed = feedGroup,
                Items = finalItemsList
            });

            return finalItemsList;
        }

        
        private async Task<IEnumerable<Database.NicoVideo>> GetItems(Database.Bookmark source)
        {
            var contentProvider = HohoemaApp.ContentProvider;
            switch (source.BookmarkType)
            {
                case Database.BookmarkType.User:
                    {
                        var items = await contentProvider.GetUserVideos(uint.Parse(source.Content), 1);
                        return items.Items.Select(x =>
                        {
                            var nicoVideo = Database.NicoVideoDb.Get(x.VideoId);
                            nicoVideo.Title = x.Title;
                            nicoVideo.ThumbnailUrl= x.ThumbnailUrl.OriginalString;
                            nicoVideo.PostedAt = x.SubmitTime;
                            nicoVideo.Length = x.Length;
                            return nicoVideo;
                        }
                        );
                    }
                case Database.BookmarkType.Mylist:
                    {
                        var items = await contentProvider.GetMylistGroupVideo(source.Content, 0, 32);

                        return items.MylistVideoInfoItems.Select(x =>
                        {
                            var nicoVideo = Database.NicoVideoDb.Get(x.Video.Id);
                            nicoVideo.Title = x.Video.Title;
                            nicoVideo.ThumbnailUrl = x.Video.ThumbnailUrl.OriginalString;
                            nicoVideo.PostedAt = x.Video.FirstRetrieve;
                            nicoVideo.Length = x.Video.Length;
                            nicoVideo.IsDeleted = x.Video.IsDeleted;
                            nicoVideo.DescriptionWithHtml = x.Video.Description;
                            nicoVideo.MylistCount = (int)x.Video.MylistCount;
                            nicoVideo.CommentCount = (int)x.Thread.GetCommentCount();
                            nicoVideo.ViewCount = (int)x.Video.ViewCount;
                            return nicoVideo;
                        });
                    }
                case Database.BookmarkType.SearchWithTag:
                    {
                        var items = await contentProvider.GetTagSearch(source.Content, 0, 32);

                        return items.VideoInfoItems.Select(x =>
                        {
                            var nicoVideo = Database.NicoVideoDb.Get(x.Video.Id);
                            nicoVideo.Title = x.Video.Title;
                            nicoVideo.ThumbnailUrl = x.Video.ThumbnailUrl.OriginalString;
                            nicoVideo.PostedAt = x.Video.FirstRetrieve;
                            nicoVideo.Length = x.Video.Length;
                            nicoVideo.IsDeleted = x.Video.IsDeleted;
                            nicoVideo.DescriptionWithHtml = x.Video.Description;
                            nicoVideo.MylistCount = (int)x.Video.MylistCount;
                            nicoVideo.CommentCount = (int)x.Thread.GetCommentCount();
                            nicoVideo.ViewCount = (int)x.Video.ViewCount;
                            return nicoVideo;
                        });
                    }
                case Database.BookmarkType.SearchWithKeyword:
                    {
                        var items = await contentProvider.GetKeywordSearch(source.Content, 0, 32);

                        return items.VideoInfoItems.Select(x =>
                        {
                            var nicoVideo = Database.NicoVideoDb.Get(x.Video.Id);
                            nicoVideo.Title = x.Video.Title;
                            nicoVideo.ThumbnailUrl = x.Video.ThumbnailUrl.OriginalString;
                            nicoVideo.PostedAt = x.Video.FirstRetrieve;
                            nicoVideo.Length = x.Video.Length;
                            nicoVideo.IsDeleted = x.Video.IsDeleted;
                            nicoVideo.DescriptionWithHtml = x.Video.Description;
                            nicoVideo.MylistCount = (int)x.Video.MylistCount;
                            nicoVideo.CommentCount = (int)x.Thread.GetCommentCount();
                            nicoVideo.ViewCount = (int)x.Video.ViewCount;
                            return nicoVideo;
                        });
                    }
                default:
                    return null;
            }
        }




        public bool RemoveFeedSource(Database.Feed target, Database.Bookmark feedSource)
        {
            var ensureFeed = GetFeedGroup(target.Id);
            if (ensureFeed != null)
            {
                var targetFeedSource = ensureFeed.Sources.FirstOrDefault(x => x.Id == feedSource.Id);
                if (ensureFeed.Sources.Remove(targetFeedSource))
                {
                    UpdateFeedGroup(ensureFeed);

                    FeedSourceRemoved?.Invoke(this, new FeedSourceRemovedEventArgs()
                    {
                        Feed = ensureFeed,
                        Bookmark = feedSource,
                    });

                    return true;
                }
            }

            return false;
        }
    }
}
