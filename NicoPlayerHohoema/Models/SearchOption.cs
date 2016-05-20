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
		public Mntone.Nico2.SortMethod SortMethod { get; set; }
		public Mntone.Nico2.SortDirection SortDirection { get; set; }


		public string ToParameterString()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this);
		}

		public static SearchOption FromParameterString(string json)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<SearchOption>(json);
		}
	}
}
