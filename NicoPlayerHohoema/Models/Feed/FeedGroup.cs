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
