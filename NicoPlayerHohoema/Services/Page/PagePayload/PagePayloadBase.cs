using Prism.Mvvm;

namespace NicoPlayerHohoema.Models
{
    public abstract class PagePayloadBase : BindableBase
	{
		public string ToParameterString()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this, new Newtonsoft.Json.JsonSerializerSettings()
			{
				TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects
			});
		}

		public static T FromParameterString<T>(string json)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
		}
	}
}
