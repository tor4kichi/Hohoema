using Mntone.Nico2;
using System;
using System.Runtime.Serialization;

namespace NicoPlayerHohoema.Models
{
    [DataContract]
	public abstract class VideoSearchOption : SearchPagePayloadContentBase, IEquatable<VideoSearchOption>
	{
        [DataMember]
        public Mntone.Nico2.Order Order { get; set; } = Order.Descending;

        [DataMember]
        public Sort Sort { get; set; } = Sort.FirstRetrieve;




		public override bool Equals(object obj)
		{
			if (obj is VideoSearchOption)
			{
				return Equals(obj as VideoSearchOption);
			}
			else
			{
				return base.Equals(obj);
			}
		}

		public bool Equals(VideoSearchOption other)
		{
			if (other == null) { return false; }

			return this.Keyword == other.Keyword
				&& this.SearchTarget == other.SearchTarget
				&& this.Order == other.Order
				&& this.Sort == other.Sort;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
