using System.Collections.Generic;

namespace NiconicoToolkit.SnapshotSearch.Filters
{
    public class CompareSearchFilter<T> : ISearchFilter
	{
		private readonly SearchFieldType _filterType;
		private readonly T _value;
		private readonly SearchFilterCompareCondition _condition;

		public CompareSearchFilter(SearchFieldType filterType, T value, SearchFilterCompareCondition condition)
		{
			_filterType = filterType;
			_value = value;
			_condition = condition;
		}

		public IEnumerable<KeyValuePair<string, string>> GetFilterKeyValues()
		{
			return new[] { new KeyValuePair<string, string>($"filters[{_filterType.GetDescription()}][{_condition.GetDescription()}]", FilterValueHelper.ToStringFilterValue(_value)) };
		}
	}



}
