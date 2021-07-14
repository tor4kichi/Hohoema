using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.SnapshotSearch.JsonFilters
{
    public sealed class AndJsonFilter : IJsonSearchFilter
	{
		private readonly IList<IJsonSearchFilter> _filters;

		public AndJsonFilter(IEnumerable<IJsonSearchFilter> filters)
		{
			_filters = filters.ToList();
		}

		public IEnumerable<KeyValuePair<string, string>> GetFilterKeyValues()
		{
			var json = JsonSerializer.Serialize(GetJsonFilterData(), JsonFilterSerializeHelper.SerializerOptions);
			return new[] { new KeyValuePair<string, string>(SearchConstants.JsonFilterParameter, json) };
		}

		public IJsonSearchFilterData GetJsonFilterData()
		{
			return new AndJsonFilterData()
			{
				Filters = _filters.Select(x => x.GetJsonFilterData() as object).ToList()
			};
		}		
	}


	public sealed class AndJsonFilterData : IJsonSearchFilterData
	{
		[JsonPropertyName("type")]
		public string Type => "and";

		[JsonPropertyName("filters")]
		public List<object> Filters { get; set; }
	}

}
