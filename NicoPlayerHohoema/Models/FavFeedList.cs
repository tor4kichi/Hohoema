using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	[DataContract]
	public class FavFeedList 
	{
		public FavFeedList()
		{
			Items = new List<FavFeedItem>();
		}



		[DataMember(Name = "item_type")]
		public FavoriteItemType FavoriteItemType { get; set; }


		[DataMember(Name = "id")]
		public string Id { get; set; }


		[DataMember(Name = "name")]
		public string Name { get; set; }


		[DataMember(Name = "update_time")]
		public DateTime UpdateTime { get; set; }


		[DataMember(Name = "items")]
		public List<FavFeedItem> Items { get; set; }


		[DataMember(Name = "source")]
		private uint _feedSourceRaw;
		public uint FeedSourceRaw
		{
			get
			{
				return _feedSourceRaw;
			}
			set
			{
				_FeedSource = (FeedSource)value;
				_feedSourceRaw = value;
			}
		}

		private FeedSource _FeedSource;
		public FeedSource FeedSource
		{
			get
			{
				return _FeedSource;
			}
			set
			{
				_FeedSource = value;
				_feedSourceRaw = (uint)value;
			}
		}


		[DataMember(Name = "deleted")]
		public bool IsDeleted { get; set; }


		/// <summary>
		/// お気に入りしたマイリストやユーザー、タグを一つにまとめるためのラベル
		/// </summary>
		[DataMember(Name = "label")]
		public string UserLabel { get; set; }
	}



	public enum FeedSource
	{
		Account = 0,
		Local = 1,
	}

	[DataContract]
	public class FavFeedItem : IEquatable<FavFeedItem>, IComparable<FavFeedItem>
	{
		[DataMember(Name = "video_id")]
		public string VideoId { get; set; }

		[DataMember(Name = "title")]
		public string Title { get; set; }

		[DataMember(Name = "checked_date")]
		public DateTime CheckedTime { get; set; }

		[DataMember(Name = "submit_date")]
		public DateTime SubmitDate { get; set; }

		public bool IsDeleted { get; set; }


		public bool IsNewItem { get; set; }

		public bool Equals(FavFeedItem other)
		{
			return VideoId == other.VideoId;
		}

		public int CompareTo(FavFeedItem other)
		{
			return (int)(this.SubmitDate.Ticks - other.SubmitDate.Ticks);
		}


	}


	public class FavFeedItemComparer : IEqualityComparer<FavFeedItem>
	{
		public static FavFeedItemComparer Default = new FavFeedItemComparer();

		public bool Equals(FavFeedItem x, FavFeedItem y)
		{
			return x.VideoId == y.VideoId;
		}

		public int GetHashCode(FavFeedItem obj)
		{
			return obj.VideoId.GetHashCode();
		}
	}
}
