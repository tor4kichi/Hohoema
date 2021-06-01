namespace NiconicoToolkit.Video.Watch
{
    public class WatchPageResponse
    {
        public WatchPageResponse(DmcWatchApiResponse watchApiResponse)
        {
            WatchApiResponse = watchApiResponse;
        }

        public WatchPageResponse(RequireActionForGuestWatchPageResponse guestWatchPageResponse)
        {
            GuestWatchPageResponse = guestWatchPageResponse;
        }

        public DmcWatchApiResponse WatchApiResponse { get; }
        public RequireActionForGuestWatchPageResponse GuestWatchPageResponse { get; }
    }


    public class DmcWatchApiResponse
    {
        public DmcWatchApiResponse(DmcWatchApiData watchApiData, DmcWatchApiEnvironment watchApiEnvironment)
        {
            WatchApiData = watchApiData;
            WatchApiEnvironment = watchApiEnvironment;
        }

        public DmcWatchApiData WatchApiData { get; }
        public DmcWatchApiEnvironment WatchApiEnvironment { get; }
    }

    public class RequireActionForGuestWatchPageResponse
    {
        public RequireActionForGuestWatchPageResponse(VideoDataForGuest videoData, TagsForGuest tags)
        {
            VideoData = videoData;
            Tags = tags;
        }

        public VideoDataForGuest VideoData { get; }
        public TagsForGuest Tags { get; }
    }

}
