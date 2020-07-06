using Hohoema.Models.Repository.Niconico;
using System;
using System.Runtime.Serialization;

namespace Hohoema.Models.Pages.PagePayload
{
    [DataContract]
	public abstract class VideoSearchOption<T> : SearchPagePayloadContentBase<T>, IEquatable<VideoSearchOption<T>>
	{
        [DataMember]
        public Order Order { get; set; } = Order.Descending;

        [DataMember]
        public Sort Sort { get; set; } = Sort.FirstRetrieve;




		public override bool Equals(object obj)
		{
			if (obj is VideoSearchOption<T>)
			{
				return Equals(obj as VideoSearchOption<T>);
			}
			else
			{
				return base.Equals(obj);
			}
		}

		public bool Equals(VideoSearchOption<T> other)
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
