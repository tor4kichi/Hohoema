using Hohoema.Models.Domain;
using System.Runtime.Serialization;
using Hohoema.Models.Domain.PageNavigation;
using NiconicoToolkit.Live.Search;
using NiconicoToolkit.Live;

namespace Hohoema.Models.UseCase.PageNavigation
{
    public class LiveSearchPagePayloadContent : SearchPagePayloadContentBase
	{
		public override SearchTarget SearchTarget => SearchTarget.Niconama;


		[DataMember]
		public bool IsTagSearch { get; set; }

		[DataMember]
		public ProviderType[] Providers { get; set; } = new ProviderType[0];

		[DataMember]
        public LiveSearchPageSortOrder Sort { get; set; } = LiveSearchPageSortOrder.RecentDesc;

		[DataMember]
		public LiveStatus LiveStatus { get; set; } = LiveStatus.Onair;

		[DataMember]
		public bool IsHideMemberOnly { get; set; }

	}
}
