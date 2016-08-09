using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class SearchOption : IEquatable<SearchOption>
	{
		public string Keyword { get; set; }
		public SearchTarget SearchTarget { get; set; }
		public Mntone.Nico2.Order Order { get; set; }
		public Mntone.Nico2.Sort Sort { get; set; }


		public string ToParameterString()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this);
		}

		public static SearchOption FromParameterString(string json)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<SearchOption>(json);
		}

		public override bool Equals(object obj)
		{
			if (obj is SearchOption)
			{
				return Equals(obj as SearchOption);
			}
			else
			{
				return base.Equals(obj);
			}
		}

		public bool Equals(SearchOption other)
		{
			if (other == null) { return false; }

			return this.Keyword == other.Keyword
				&& this.SearchTarget == other.SearchTarget
				&& this.Order == other.Order
				&& this.Sort == other.Sort;
		}
	}
}
