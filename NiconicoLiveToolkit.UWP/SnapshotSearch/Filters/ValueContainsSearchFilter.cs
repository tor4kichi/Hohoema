using System.Collections.Generic;
using System.Linq;

namespace NiconicoToolkit.SnapshotSearch.Filters
{
    public sealed class ValueContainsSearchFilter<T> : ISearchFilter
	{
		SearchFieldType _filterType;
		IEnumerable<T> _values;

		public ValueContainsSearchFilter(SearchFieldType filterType, params T[] values)
		{
			_filterType = (SearchFieldType)filterType;
			_values = values;
		}

		public IEnumerable<KeyValuePair<string, string>> GetFilterKeyValues()
		{
			return _values.Select((x, i) => new KeyValuePair<string, string>($"filters[{_filterType.GetDescription()}][{i}]", x.ToString()));
		}
	}



}
