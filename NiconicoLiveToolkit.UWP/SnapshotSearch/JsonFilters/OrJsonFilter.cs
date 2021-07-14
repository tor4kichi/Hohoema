using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.SnapshotSearch.JsonFilters
{
    public sealed class OrJsonFilter : IJsonSearchFilter
    {
        private readonly IList<IJsonSearchFilter> _filters;

        public OrJsonFilter(IEnumerable<IJsonSearchFilter> filters)
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
			return new OrJsonFilterData()
			{
				Filters = _filters.Select(x => x.GetJsonFilterData() as object).ToList()
			};
		}
    }


	public sealed class OrJsonFilterData : IJsonSearchFilterData
	{
		[JsonPropertyName("type")]
		public string Type => "or";

		[JsonPropertyName("filters")]
		public List<object> Filters { get; set; }
	}
}
