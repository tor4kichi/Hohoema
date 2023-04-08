#nullable enable
using NiconicoToolkit.SearchWithCeApi.Video;
using System;
using System.Runtime.Serialization;

namespace Hohoema.Services.Navigations;

[DataContract]
	public abstract class VideoSearchOption : SearchPagePayloadContentBase, IEquatable<VideoSearchOption>
	{
    [DataMember]
    public VideoSortOrder Order { get; set; } = VideoSortOrder.Desc;

    [DataMember]
    public VideoSortKey Sort { get; set; } = VideoSortKey.FirstRetrieve;




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
