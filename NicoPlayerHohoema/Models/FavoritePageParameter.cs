using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class FavoritePageParameter
	{
		public FavoritePageParameter()
		{

		}

		public string Id { get; set; }
		public FavoriteItemType ItemType { get; set; }


		public static string ToJson(FavoritePageParameter parameter)
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(parameter);
		}

		public static FavoritePageParameter FromJson(string json)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<FavoritePageParameter>(json);
		}

		public string ToJson()
		{
			return ToJson(this);
		}
	}
}
