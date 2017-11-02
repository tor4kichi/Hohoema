using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NicoPlayerHohoema.Helpers;

namespace NicoPlayerHohoema.Models
{
	public interface IFeedGroup
	{
        Guid Id { get; }
        string Label { get; }
        DateTime UpdateTime { get; }

        IList<FeedItem> FeedItems { get; }
		IReadOnlyList<IFeedSource> FeedSourceList { get; }

		IFeedSource AddMylistFeedSource(string name, string mylistGroupId);
		IFeedSource AddTagFeedSource(string tag);
		IFeedSource AddUserFeedSource(string name, string userId);
		bool ExistFeedSource(FollowItemType itemType, string id);
		void ForceMarkAsRead();
		int GetUnreadItemCount();
		bool MarkAsRead(string videoId);

        bool IsRefreshRequired { get; }

        Task Refresh();
		void RemoveUserFeedSource(IFeedSource feedSource);
		Task<bool> Rename(string newLabel);
	}
}