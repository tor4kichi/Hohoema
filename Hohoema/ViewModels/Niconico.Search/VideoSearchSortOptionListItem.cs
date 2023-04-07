using NiconicoToolkit.SearchWithCeApi.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Search
{
	public class SearchSortOptionListItem
	{
		public VideoSortOrder Order { get; set; }
		public VideoSortKey Sort { get; set; }
		public string Label { get; set; }
	}

}
