using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Helpers
{



	[DataContract]
	public sealed class LisenceSummary
	{
		[DataMember(Name = "items")]
		public List<LisenceItem> Items { get; set; }


		public static async Task<LisenceSummary> Load()
		{
			string LisenceSummaryFilePath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path + "\\Assets\\LibLisencies\\_lisence_summary.json";

			var file = await StorageFile.GetFileFromPathAsync(LisenceSummaryFilePath);

			var text = await FileIO.ReadTextAsync(file);

			return JsonConvert.DeserializeObject<LisenceSummary>(text);
		}

	}


	[DataContract]
	public sealed class LisenceItem
	{
		[DataMember(Name = "name")]
		public string Name { get; set; }

		[DataMember(Name = "site")]
		private string __Site { get; set; }

		[DataMember(Name = "author")]
		public List<string> Authors { get; set; }

		[DataMember(Name = "lisence_type")]
		private string __LisenceType { get; set; }

		[DataMember(Name = "lisence_page_url")]
		private string __LisencePageUrl { get; set; }



		private Uri _Site;
		public Uri Site
		{
			get
			{
				return _Site
					?? (_Site = new Uri(__Site));
			}
		}


		private LisenceType? _LisenceType;
		public LisenceType? LisenceType
		{
			get
			{
				return _LisenceType
					?? (_LisenceType = (LisenceType) Enum.Parse(typeof(LisenceType), __LisenceType));
			}
		}

		private Uri _LisencePageUrl;
		public Uri LisencePageUrl
		{
			get
			{
				return _LisencePageUrl
					?? (_LisencePageUrl = new Uri(__LisencePageUrl));
			}
		}
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
