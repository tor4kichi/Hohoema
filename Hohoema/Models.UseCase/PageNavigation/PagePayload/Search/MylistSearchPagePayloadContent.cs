using Hohoema.Models.Domain;
using Hohoema.Models.Domain.PageNavigation;
using System.Runtime.Serialization;

namespace Hohoema.Models.UseCase.PageNavigation
{
    public class MylistSearchPagePayloadContent : SearchPagePayloadContentBase
	{
		public override SearchTarget SearchTarget => SearchTarget.Mylist;

        [DataMember]
        public Mntone.Nico2.Order Order { get; set; } = Mntone.Nico2.Order.Descending;

        [DataMember]
        public Mntone.Nico2.Sort Sort { get; set; } = Mntone.Nico2.Sort.MylistPopurarity;
	}
}
