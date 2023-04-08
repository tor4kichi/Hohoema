using Hohoema.Models.PageNavigation;
using NiconicoToolkit.Live;
using NiconicoToolkit.SearchWithPage.Live;
using System.Runtime.Serialization;

namespace Hohoema.Services.Navigations;

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
