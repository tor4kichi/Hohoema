using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Community
{
    public sealed class CommunityClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _options;

        internal CommunityClient(NiconicoContext context)
        {
            _context = context;
            _options = new JsonSerializerOptions() 
            {
                PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy(),
                Converters =
                {
                    new JsonStringEnumMemberConverter(new JsonSnakeCaseNamingPolicy()),
                }
            };
        }


        public static class Urls
        {
            public const string CommunityPageUrl = "http://com.nicovideo.jp/community/";

            public static string MakeCommunityPageUrl(string communityId)
            {
                return $"{CommunityPageUrl}{communityId}";
            }


            public const string CommunityApiUrl = "https://com.nicovideo.jp/api/v1/communities/";

            public static string MakeAuthorityApiUrl(string communityId)
            {
                string nonPrefixCommunityId = ContentIdHelper.EnsureNonPrefixCommunityId(communityId);
                return new StringBuilder(CommunityApiUrl)
                    .Append(nonPrefixCommunityId)
                    .Append("/authority.json")
                    .ToString();
            }

            public static string MakeCommuynityVideoListApiUrl(string communityId, int? offset, int? limit, CommunityVideoSortKey? sortKey, CommunityVideoSortOrder? sortOrder)
            {
                // ex) https://com.nicovideo.jp/api/v1/communities/540200/contents/videos.json?limit=20&offset=0&sort=c&direction=d&_=1623371150520

                string nonPrefixCommunityId = ContentIdHelper.EnsureNonPrefixCommunityId(communityId);
                NameValueCollection dict = new();
                dict.AddIfNotNull("offset", offset);
                dict.AddIfNotNull("limit", limit);
                dict.AddEnumIfNotNull("sort", sortKey);
                dict.AddEnumIfNotNull("direction", sortOrder);

                return new StringBuilder("https://com.nicovideo.jp/api/v1/communities/")
                    .Append(nonPrefixCommunityId)
                    .Append("/contents/videos.json")
                    .AppendQueryString(dict)
                    .ToString();
            }



            public static string MakeCommuynityVideoListItemsApiUrl(IEnumerable<string> videoIds)
            {
                // ex) https://com.nicovideo.jp/api/v1/videos.json?video_ids=sm26963608,sm26963450,sm26963253

                return new StringBuilder("https://com.nicovideo.jp/api/v1/videos.json?video_ids=")
                    .AppendJoin(Uri.EscapeDataString(","), videoIds)
                    .ToString();
            }



            public static string MakeCommunityLiveApiUrl(string communityId, int? offset, int? limit)
            {
                // ex) https://com.nicovideo.jp/api/v1/communities/1408350/lives.json?limit=30&offset=0

                string nonPrefixCommunityId = ContentIdHelper.EnsureNonPrefixCommunityId(communityId);
                NameValueCollection dict = new();
                dict.AddIfNotNull("offset", offset);
                dict.AddIfNotNull("limit", limit);

                return new StringBuilder("https://com.nicovideo.jp/api/v1/communities/")
                    .Append(nonPrefixCommunityId)
                    .Append("/lives.json")
                    .AppendQueryString(dict)
                    .ToString();
            }
        }



        public Task<CommunityAuthorityResponse> GetCommunityAuthorityForLoginUserAsync(string communityId)
        {
            return _context.GetJsonAsAsync<CommunityAuthorityResponse>(Urls.MakeAuthorityApiUrl(communityId));
        }

        public Task<CommunityVideoResponse> GetCommunityVideoListAsync(string communityId, int? offset = 0, int? limit = 20, CommunityVideoSortKey? sortKey = null, CommunityVideoSortOrder? sortOrder = null)
        {
            return _context.GetJsonAsAsync<CommunityVideoResponse>(Urls.MakeCommuynityVideoListApiUrl(communityId, offset, limit, sortKey, sortOrder), _options);
        }

        public Task<CommunityVideoListItemsResponse> GetCommunityVideoListItemsAsync(IEnumerable<string> videoIds)
        {
            return _context.GetJsonAsAsync<CommunityVideoListItemsResponse>(Urls.MakeCommuynityVideoListItemsApiUrl(videoIds), _options);
        }

        public Task<CommunityLiveResponse> GetCommunityLiveAsync(string communityId, int? offset = 0, int? limit = 30)
        {
            return _context.GetJsonAsAsync<CommunityLiveResponse>(Urls.MakeCommunityLiveApiUrl(communityId, offset, limit), _options);
        }
    }

   
}
