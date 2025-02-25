#nullable enable
using Hohoema.Models.Niconico.Search;
using NiconicoToolkit.Search.Video;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Hohoema.Services.Navigations;

[DataContract]
public abstract class VideoSearchOption : SearchPagePayloadContentBase, IEquatable<VideoSearchOption>
{
	[DataMember]
	public SortOrder Order { get; set; } = SortOrder.Desc;

	[DataMember]
	public SortKey Sort { get; set; } = SortKey.RegisteredAt;




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
