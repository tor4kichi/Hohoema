using System.Collections.Generic;
using System.Linq;

namespace NiconicoToolkit.SnapshotSearch.Filters
{
    public class CompositionSearchFilter : ISearchFilter
	{
		private readonly IEnumerable<ISearchFilter> _filters;

		public CompositionSearchFilter(IEnumerable<ISearchFilter> filters)
		{
			_filters = filters;
		}

		public IEnumerable<KeyValuePair<string, string>> GetFilterKeyValues()
		{
			return _filters.SelectMany(x => x.GetFilterKeyValues());

		}
	}



}
