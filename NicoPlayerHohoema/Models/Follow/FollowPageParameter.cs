using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class FollowPageParameter
	{
		public FollowPageParameter()
		{

		}

		public string Id { get; set; }
		public FollowItemType ItemType { get; set; }


		public static string ToJson(FollowPageParameter parameter)
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(parameter);
		}

		public static FollowPageParameter FromJson(string json)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<FollowPageParameter>(json);
		}

		public string ToJson()
		{
			return ToJson(this);
		}
	}
}
