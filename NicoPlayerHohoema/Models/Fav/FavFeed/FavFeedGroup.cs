using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Fav.FavFeed
{
	// ユーザーが指定したFavItemを束ねて、動画Feedを生成する
	public class FavFeedGroup
	{
		/*
	private async Task MergeFavFeedList(FavInfo feedList, List<FavFeedItem> items, DateTime updateTime)
		{
			var exceptItems = items.Except(feedList.Items, FavFeedItemComparer.Default).ToList();

			var addedItems = exceptItems.Where(x => x.CheckedTime == updateTime).ToList();

			var removedItems = exceptItems.Except(addedItems, FavFeedItemComparer.Default);

			foreach (var addItem in addedItems)
			{
				addItem.IsUnread = true;

				// 投稿日時が初期化されていない場合はThumbnailInfoから拾ってくる

				if (addItem.SubmitDate == default(DateTime))
				{
					try
					{
						var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(addItem.VideoId);
						var thumbnail = await nicoVideo.GetThumbnailResponse();

						addItem.SubmitDate = thumbnail.PostedAt.DateTime;
					}
					catch (Exception ex)
					{
						Debug.Fail("UserFeedItem 更新中、NicoVideoオブジェクトの取得に失敗しました。", ex.Message);
					}
				}
			

				feedList.Items.Add(addItem);

				AddFavFeedEvent?.Invoke(addItem);
			}


			foreach (var removedItem in removedItems)
			{
				var item = feedList.Items.SingleOrDefault(x => x.VideoId == removedItem.VideoId);
				if (item != null)
				{
					item.IsDeleted = true;
					feedList.Items.Remove(item);
				}
			}

			feedList.Items.Sort();

			feedList.UpdateTime = updateTime;
		}

 
		*/
	}
}
