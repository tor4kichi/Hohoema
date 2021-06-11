#if WINDOWS_UWP
#else
using System.Net.Http;
using System.Net.Http.Headers;
#endif

namespace NiconicoToolkit
{
    public static class NiconicoUrls
    {
        public const string NicoHomePageUrl = "https://www.nicovideo.jp/";
        public const string NicoLivePageUrl = "https://live2.nicovideo.jp/";

        public const string NvApiV1Url = "https://nvapi.nicovideo.jp/v1/";
        public const string NvApiV2Url = "https://nvapi.nicovideo.jp/v2/";
        public const string PublicApiV1Url = "https://public.api.nicovideo.jp/v1/";
        public const string CeApiV1Url = "http://api.ce.nicovideo.jp/api/v1/";
        public const string CeNicoApiV1Url = "http://api.ce.nicovideo.jp/nicoapi/v1/";
        public const string LiveApiV1Url = "https://api.live2.nicovideo.jp/api/v1/";

        public const string ChannelPageUrl = "https://ch.nicovideo.jp/";
        public const string ChannelPublicApiV2Url = "https://public-api.ch.nicovideo.jp/v2/";
        public const string ChannelApiUrl = $"{ChannelPageUrl}api/";


        public const string CommunityPageUrl = "https://com.nicovideo.jp/";
        public const string CommunityV1ApiUrl = $"{CommunityPageUrl}api/v1/";

        public const string WatchPageUrl = $"{NicoHomePageUrl}watch/";

        public static string MakeWatchPageUrl(string videoId)
        {
            return $"{WatchPageUrl}{videoId}";
        }


        public const string LiveWatchPageUrl = $"{NicoLivePageUrl}watch/";

        public static string MakeLiveWatchPageUrl(string liveId)
        {
            return $"{LiveWatchPageUrl}{liveId}";
        }




        public static string MakeUserPageUrl(int userId)
        {
            return MakeUserPageUrl(userId);
        }

        public static string MakeUserPageUrl(uint userId)
        {
            return MakeUserPageUrl(userId);
        }

        public static string MakeUserPageUrl(string userId)
        {
            return MakeUserPageUrl(userId);
        }

        internal static string MakeUserPageUrl<IdType>(IdType userId)
        {
            return $"{NicoHomePageUrl}user/{userId}";
        }


        public static string MakeCommunityPageUrl(string communityId)
        {
            return $"{CommunityPageUrl}community/{communityId}";
        }


        public static string MakeMylistPageUrl(string mylistId)
        {
            return $"{NicoHomePageUrl}mylist/{mylistId}";
        }



        public static string MakeChannelPageUrl(string channelId)
        {
            var (isScreenName, checkedChannelId) = ContentIdHelper.EnsurePrefixChannelIdOrScreenName(channelId);
            return isScreenName
                ? $"{ChannelPageUrl}{checkedChannelId}"
                : $"{ChannelPageUrl}channel/{checkedChannelId}"
                ;
        }

        public static string MakeChannelVideoPageUrl(string channelId)
        {
            return $"{MakeChannelPageUrl(channelId)}/video";
        }
    }

}
