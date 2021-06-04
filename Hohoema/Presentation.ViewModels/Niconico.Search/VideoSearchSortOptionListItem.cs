using Mntone.Nico2;
using NiconicoToolkit.SearchWithCeApi.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Search
{
	public class SearchSortOptionListItem
	{
		public VideoSortOrder Order { get; set; }
		public VideoSortKey Sort { get; set; }
		public string Label { get; set; }
	}

	public class MylistSearchSortOptionListItem
	{
		public Order Order { get; set; }
		public Sort Sort { get; set; }
		public string Label { get; set; }

	}
}
