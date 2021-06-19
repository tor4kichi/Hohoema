using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;
using System.Text.Json.Serialization;

namespace Hohoema.Models.Domain
{



	[DataContract]
	public sealed class LisenceSummary
	{
		[JsonPropertyName("items")]
		public List<LisenceItem> Items { get; set; }


		public static async Task<LisenceSummary> Load()
		{
			string LisenceSummaryFilePath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path + "\\Assets\\LibLisencies\\_lisence_summary.json";

			var file = await StorageFile.GetFileFromPathAsync(LisenceSummaryFilePath);

			using (var stream = await file.OpenAsync(FileAccessMode.Read))
			using (var readStream = stream.AsStreamForRead())
			{
				return await System.Text.Json.JsonSerializer.DeserializeAsync<LisenceSummary>(readStream);
            }
		}
	}


	
	public sealed class LisenceItem
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("site")]
		public Uri Site { get; set; }

		[JsonPropertyName("author")]
		public List<string> Authors { get; set; }

		[JsonPropertyName("lisence_type")]
		public string LisenceType { get; set; }

		[JsonPropertyName("lisence_page_url")]
		public Uri LisencePageUrl { get; set; }
	}


	public enum LisenceType
	{
		MIT,
		MS_PL,
		Apache_v2,
		Simplified_BSD,
		CC_BY_40,
		SIL_OFL_v1_1,
		GPL_v3,
	}

}
