using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NicoPlayerHohoema.Util;

namespace NicoPlayerHohoema.Models
{
	// TODO: 整理
	public interface IFeedGroup
	{
		List<FavFeedItem> FeedItems { get; }
		FeedManager FeedManager { get; }
		IReadOnlyList<IFeedSource> FeedSourceList { get; }
		HohoemaApp HohoemaApp { get; }
		Guid Id { get; }
		bool IsNeedRefresh { get; }
		string Label { get; }
		DateTime UpdateTime { get; }

		IFeedSource AddMylistFeedSource(string name, string mylistGroupId);
		IFeedSource AddTagFeedSource(string tag);
		IFeedSource AddUserFeedSource(string name, string userId);
		bool ExistFeedSource(FavoriteItemType itemType, string id);
		void ForceMarkAsRead();
		int GetUnreadItemCount();
		bool MarkAsRead(string videoId);
		Task<bool> LoadFeedStream(FileAccessor<List<FavFeedItem>> fileAccessor);
		Task Refresh();
		void RemoveUserFeedSource(IFeedSource feedSource);
		Task<bool> Rename(string newLabel);
	}
}