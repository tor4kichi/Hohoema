using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.SnapshotSearch.JsonFilters
{
    public sealed class EqaulJsonFilter<T> : IJsonSearchFilter
	{
		private readonly SearchFieldType _filterType;
		private readonly T _value;
        
        public EqaulJsonFilter(SearchFieldType filterType, T value)
		{
			_filterType = filterType;
			_value = value;
		}

		public IEnumerable<KeyValuePair<string, string>> GetFilterKeyValues()
		{
			var json = JsonSerializer.Serialize(GetJsonFilterData(), JsonFilterSerializeHelper.SerializerOptions);
			return new[] { new KeyValuePair<string, string>(SearchConstants.JsonFilterParameter, json) };
		}

        public IJsonSearchFilterData GetJsonFilterData()
        {
			return new EqualJsonFilterData()
			{
				Field = _filterType.GetDescription(),
				Value = FilterValueHelper.ToStringFilterValue(_value)
			};
		}
	}


	public sealed class EqualJsonFilterData : IJsonSearchFilterData
	{
		[JsonPropertyName("type")]
		public string Type => "equal";

		[JsonPropertyName("field")]
		public string Field { get; set; }

		[JsonPropertyName("value")]
		public string Value { get; set; }
	}


}
