﻿using Mntone.Nico2.Live;
using Mntone.Nico2.Searches.Live;
using Hohoema.Models;
using System.Runtime.Serialization;

namespace Hohoema.Models.Pages.PagePayload
{
    public class LiveSearchPagePayloadContent : SearchPagePayloadContentBase<LiveSearchPagePayloadContent>
	{
		public override SearchTarget SearchTarget => SearchTarget.Niconama;


		[DataMember]
		public bool IsTagSearch { get; set; }

		[DataMember]
		public Mntone.Nico2.Live.CommunityType? Provider { get; set; }

        [DataMember]
        public LiveSearchSortType Sort { get; set; } = LiveSearchSortType.StartTime | LiveSearchSortType.SortDecsending;

		[DataMember]
		public StatusType LiveStatus { get; set; } = StatusType.OnAir;

		[DataMember]
		public bool IsExcludeCommunityMemberOnly { get; set; }

	}
}
