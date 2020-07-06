using Hohoema.Models.Repository.Niconico;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels
{
	public class SearchSortOptionListItem
	{
		public Order Order { get; set; }
		public Sort Sort { get; set; }
		public string Label { get; set; }

	}

}
