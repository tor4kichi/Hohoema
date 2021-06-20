#if WINDOWS_UWP
#else
using System.Net.Http;
using System.Net.Http.Headers;
#endif

using NiconicoToolkit.Channels;
using NiconicoToolkit.User;
using System;
using System.Text.RegularExpressions;

namespace NiconicoToolkit
{
    public static class NiconicoUrls
    {
        public const string NicoDomain = "nicovideo.jp";

        public const string NicoHomeHost = $"www.{NicoDomain}";
        public const string NicoHomePageUrl = $"https://{NicoHomeHost}/";


        public const string NicoLive2PageUrl = "https://live2.nicovideo.jp/";

        public const string NicoLiveHost = $"live.{NicoDomain}";
        public const string NicoLivePageUrl = $"https://{NicoLiveHost}/";

        public const string NvApiV1Url = "https://nvapi.nicovideo.jp/v1/";
        public const string NvApiV2Url = "https://nvapi.nicovideo.jp/v2/";
        public const string PublicApiV1Url = "https://public.api.nicovideo.jp/v1/";
        public const string CeApiV1Url = "http://api.ce.nicovideo.jp/api/v1/";
        public const string CeNicoApiV1Url = "http://api.ce.nicovideo.jp/nicoapi/v1/";
        public const string LiveApiV1Url = "https://api.live2.nicovideo.jp/api/v1/";

        public const string NicoChannelHost = $"ch.{NicoDomain}";
        public const string ChannelPageUrl = $"https://{NicoChannelHost}/";
        public const string ChannelPublicApiV2Url = "https://public-api.ch.nicovideo.jp/v2/";
        public const string ChannelApiUrl = $"{ChannelPageUrl}api/";

        public const string CommunityHost = $"com.{NicoDomain}";
        public const string CommunityPageUrl = $"https://{CommunityHost}/";
        public const string CommunityV1ApiUrl = $"{CommunityPageUrl}api/v1/";

        public const string IchibaPageUrl = "http://ichiba.nicovideo.jp/";


        public const string WatchPageUrl = $"{NicoHomePageUrl}watch/";

        public static string MakeWatchPageUrl(string videoId)
        {
            return $"{WatchPageUrl}{videoId}";
        }


        public const string LiveWatchPageUrl = $"{NicoLive2PageUrl}watch/";

        public static string MakeLiveWatchPageUrl(Live.LiveId liveId)
        {
            return $"{LiveWatchPageUrl}{liveId}";
        }


        public static string MakeUserPageUrl(UserId userId)
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



        public static string MakeChannelPageUrl(string channelIdOrScreenName)
        {
            var (isScreenName, checkedChannelId) = ContentIdHelper.EnsurePrefixChannelIdOrScreenName(channelIdOrScreenName);
            return isScreenName
                ? $"{ChannelPageUrl}{checkedChannelId}"
                : $"{ChannelPageUrl}channel/{checkedChannelId}"
                ;
        }

        public static string MakeChannelPageUrl(ChannelId channelId)
        {
            return $"{ChannelPageUrl}channel/{channelId}";
        }

        public static string MakeChannelVideoPageUrl(string channelId)
        {
            return $"{MakeChannelPageUrl(channelId)}/video";
        }

        public static string MakeChannelVideoPageUrl(ChannelId channelId)
        {
            return $"{MakeChannelPageUrl(channelId)}/video";
        }










        static readonly Regex NicoContentRegex = new Regex("https?:\\/\\/([\\w\\W]*?)\\/((\\w*)\\/)*([\\w-]*)");

        public static NiconicoId? ExtractNicoContentId(Uri url)
        {
            var match = NicoContentRegex.Match(url.OriginalString);
            if (match.Success)
            {
                var hostNameGroup = match.Groups[1];
                var contentTypeGroup = match.Groups[3];
                var contentIdGroup = match.Groups[4];

                var contentId = contentIdGroup.Value;

                if (hostNameGroup.Success)
                {
                    if (hostNameGroup.Value == NiconicoUrls.NicoHomeHost)
                    {
                        var contentType = contentTypeGroup.Value;
                        switch (contentType)
                        {
                            case "watch":
                                return new NiconicoId(contentId);

                            case "mylist":
                                return new NiconicoId(contentId, NiconicoIdType.Mylist);

                            case "user":
                                return new NiconicoId(contentId, NiconicoIdType.User);

                            case "series":
                                return new NiconicoId(contentId, NiconicoIdType.Series);
                        }
                    }
                    else if (hostNameGroup.Value == NiconicoUrls.NicoLiveHost)
                    {
                        return new NiconicoId(contentId, NiconicoIdType.Live);
                    }
                    else if (hostNameGroup.Value == NiconicoUrls.NicoChannelHost)
                    {
                        if (ContentIdHelper.IsChannelId(contentIdGroup.Value, allowNonPrefixId: false))
                        {
                            return new NiconicoId(contentId, NiconicoIdType.Channel);
                        }
                        else
                        {
                            return new NiconicoId(contentId, NiconicoIdType.Channel);
                        }
                    }
                    else if (hostNameGroup.Value == NiconicoUrls.CommunityHost)
                    {
                        var contentType = contentTypeGroup.Value;
                        switch (contentType)
                        {
                            case "community":
                                return new NiconicoId(contentId, NiconicoIdType.Community);
                        }
                    }
                }

                if (ContentIdHelper.IsVideoId(contentId, allowNonPrefixId: false))
                {
                    return new NiconicoId(contentId);
                }

                if (ContentIdHelper.IsLiveId(contentId, allowNonPrefixId: false))
                {
                    return new NiconicoId(contentId, NiconicoIdType.Live);
                }
            }

            return null;
        }
    }
}
