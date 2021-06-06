using System;

namespace NiconicoToolkit.SearchWithPage.Video
{
    public struct RangeDatePair
    {
		public RangeDatePair(DateTime start, DateTime end)
        {
			VideoSearchSubClient.ThrowExceptionIfInvalidRangeDatePair(start, end);

			Start = start;
			End = end;
        }
		public DateTime Start { get; }
		public DateTime End { get; }
	}

	

}
