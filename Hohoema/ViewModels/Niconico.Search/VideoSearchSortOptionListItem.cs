#nullable enable
using NiconicoToolkit.SearchWithCeApi.Video;

namespace Hohoema.ViewModels.Niconico.Search;

public class SearchSortOptionListItem
	{
		public VideoSortOrder Order { get; set; }
		public VideoSortKey Sort { get; set; }
		public string Label { get; set; }
	}
