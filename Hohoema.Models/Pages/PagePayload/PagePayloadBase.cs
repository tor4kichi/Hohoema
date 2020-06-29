
namespace Hohoema.Models.Pages.PagePayload
{
    public abstract class PagePayloadBase<T>
	{
		public string ToParameterString()
		{
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, new Newtonsoft.Json.JsonSerializerSettings()
			{
				TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects
			});
		}

		public static T FromParameterString(string json)
		{
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                return default(T);
            }
        }
	}
}
