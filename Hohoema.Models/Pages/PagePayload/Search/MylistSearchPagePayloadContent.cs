using Hohoema.Models;
using System.Runtime.Serialization;

namespace Hohoema.Models.Pages.PagePayload
{
    public class MylistSearchPagePayloadContent : SearchPagePayloadContentBase<MylistSearchPagePayloadContent>
	{
		public override SearchTarget SearchTarget => SearchTarget.Mylist;

        [DataMember]
        public Mntone.Nico2.Order Order { get; set; } = Mntone.Nico2.Order.Descending;

        [DataMember]
        public Mntone.Nico2.Sort Sort { get; set; } = Mntone.Nico2.Sort.MylistPopurarity;
	}
}
