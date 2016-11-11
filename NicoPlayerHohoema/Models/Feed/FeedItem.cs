using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	[DataContract]
	public class FeedItem : BindableBase, IEquatable<FeedItem>, IComparable<FeedItem>
	{

		string _VideoId;

		[DataMember(Name = "video_id")]
		public string VideoId
		{
			get { return _VideoId; }
			set { SetProperty(ref _VideoId, value); }
		}


		string _Title;

		[DataMember(Name = "title")]
		public string Title
		{
			get { return _Title; }
			set { SetProperty(ref _Title, value); }
		}



		DateTime _CheckedTime;

		[DataMember(Name = "checked_date")]
		public DateTime CheckedTime
		{
			get { return _CheckedTime; }
			set { SetProperty(ref _CheckedTime, value); }
		}


		DateTime _SubmitDate;

		[DataMember(Name = "submit_date")]
		public DateTime SubmitDate
		{
			get { return _SubmitDate; }
			set { SetProperty(ref _SubmitDate, value); }
		}


		bool _IsDeleted;

		public bool IsDeleted
		{
			get { return _IsDeleted; }
			set { SetProperty(ref _IsDeleted, value); }
		}



		bool _IsUnread;

		[DataMember(Name = "is_unread")]
		public bool IsUnread
		{
			get { return _IsUnread; }
			set { SetProperty(ref _IsUnread, value); }
		}



		public bool Equals(FeedItem other)
		{
			return VideoId == other.VideoId;
		}

		public int CompareTo(FeedItem other)
		{
			return (int)(this.SubmitDate.Ticks - other.SubmitDate.Ticks);
		}


	}
}
