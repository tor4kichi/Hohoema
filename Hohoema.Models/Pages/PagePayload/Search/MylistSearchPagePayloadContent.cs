using Hohoema.Models;
using Hohoema.Models.Repository.Niconico;
using System.Runtime.Serialization;

namespace Hohoema.Models.Pages.PagePayload
{
    public class MylistSearchPagePayloadContent : SearchPagePayloadContentBase<MylistSearchPagePayloadContent>
	{
		public override SearchTarget SearchTarget => SearchTarget.Mylist;

        [DataMember]
        public Order Order { get; set; } = Order.Descending;

        [DataMember]
        public Sort Sort { get; set; } = Sort.MylistPopurarity;
	}
}
