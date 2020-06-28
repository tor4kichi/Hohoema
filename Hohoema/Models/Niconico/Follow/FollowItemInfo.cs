using Mntone.Nico2;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models
{
	[DataContract]
	public class FollowItemInfo 
	{
		public FollowItemInfo()
		{
		}



		[DataMember(Name = "item_type")]
		public FollowItemType FollowItemType { get; set; }


		[DataMember(Name = "id")]
		public string Id { get; set; }


		[DataMember(Name = "name")]
		public string Name { get; set; }


		[DataMember(Name = "update_time")]
		public DateTime UpdateTime { get; set; }

        [DataMember(Name = "thumbnail")]
        public string ThumbnailUrl { get; set; }


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
