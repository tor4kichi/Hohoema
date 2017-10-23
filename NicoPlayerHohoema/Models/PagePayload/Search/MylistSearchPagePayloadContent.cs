using System.Runtime.Serialization;

namespace NicoPlayerHohoema.Models
{
    public class MylistSearchPagePayloadContent : SearchPagePayloadContentBase
	{
		public override SearchTarget SearchTarget => SearchTarget.Mylist;

		[DataMember]
		public Mntone.Nico2.Order Order { get; set; }

		[DataMember]
		public Mntone.Nico2.Sort Sort { get; set; }
	}
}
