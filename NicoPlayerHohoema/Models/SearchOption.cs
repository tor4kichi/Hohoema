using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class SearchOption
	{
		public string Keyword { get; set; }
		public SearchTarget SearchTarget { get; set; }
		public Mntone.Nico2.Videos.Search.SearchSortMethod SortMethod { get; set; }
		public Mntone.Nico2.SortDirection SortDirection { get; set; }
	}
}
