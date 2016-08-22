using Mntone.Nico2;
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
	public class FavInfo
	{
		public FavInfo()
		{
		}



		[DataMember(Name = "item_type")]
		public FavoriteItemType FavoriteItemType { get; set; }


		[DataMember(Name = "id")]
		public string Id { get; set; }


		[DataMember(Name = "name")]
		public string Name { get; set; }


		[DataMember(Name = "update_time")]
		public DateTime UpdateTime { get; set; }


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


		[OnDeserialized]
		public void OnSeralized(StreamingContext context)
		{
//			foreach (var item in Items)
			{
//				item.ParentList = this;
			}
		}
	}





	


	
}
