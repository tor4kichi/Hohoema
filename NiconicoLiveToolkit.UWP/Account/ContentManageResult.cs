using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Windows.Web.Http;

namespace NiconicoToolkit.Account
{
    public enum ContentManageResult
    {
        Failed,
        Exist,
        Success,
    }

	public class ContentManageResultError
	{
		public string code { get; set; }
	}

	public class ContentManageResultData
	{
		[JsonPropertyName("error")]
		public ContentManageResultError Error { get; set; }

		[JsonPropertyName("Srror")]
		public string Status { get; set; }

		[JsonPropertyName("delete_count")]
		[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
		public long? DeleteCount { get; set; }
	}


    public static class ContentManageResultHelper
    {
		public static ContentManageResult ToContentManagerResult(this HttpStatusCode code)
        {
            if (code == HttpStatusCode.Conflict)
            {
                return ContentManageResult.Exist;
            }
            else if (HttpStatusCodeHelper.IsSuccessStatusCode((int)code))
            {
                return ContentManageResult.Success;
            }
            else
            {
                return ContentManageResult.Failed;
            }
        }
	}

}
