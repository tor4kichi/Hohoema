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


        internal static class Urls
        {
            public const string CommunityV1CommunitiesApiUrl = $"{NiconicoUrls.CommunityV1ApiUrl}communities/";
        }



        public Task<CommunityAuthorityResponse> GetCommunityAuthorityForLoginUserAsync(string communityId)
        {
            string nonPrefixCommunityId = ContentIdHelper.EnsureNonPrefixCommunityId(communityId);
            var url = new StringBuilder(Urls.CommunityV1CommunitiesApiUrl)
                .Append(nonPrefixCommunityId)
                .Append("/authority.json")
                .ToString();

            return _context.GetJsonAsAsync<CommunityAuthorityResponse>(url);
        }

        public Task<CommunityVideoResponse> GetCommunityVideoListAsync(string communityId, int? offset = 0, int? limit = 20, CommunityVideoSortKey? sortKey = null, CommunityVideoSortOrder? sortOrder = null)
        {
            // ex) https://com.nicovideo.jp/api/v1/communities/540200/contents/videos.json?limit=20&offset=0&sort=c&direction=d&_=1623371150520

            string nonPrefixCommunityId = ContentIdHelper.EnsureNonPrefixCommunityId(communityId);
            NameValueCollection dict = new();
            dict.AddIfNotNull("offset", offset);
            dict.AddIfNotNull("limit", limit);
            dict.AddEnumIfNotNullWithDescription("sort", sortKey);
            dict.AddEnumIfNotNullWithDescription("direction", sortOrder);

            var url = new StringBuilder(Urls.CommunityV1CommunitiesApiUrl)
                .Append(nonPrefixCommunityId)
                .Append("/contents/videos.json")
                .AppendQueryString(dict)
                .ToString();

            return _context.GetJsonAsAsync<CommunityVideoResponse>(url, _options);
        }

        public Task<CommunityVideoListItemsResponse> GetCommunityVideoListItemsAsync(IEnumerable<string> videoIds)
        {
            // ex) https://com.nicovideo.jp/api/v1/videos.json?video_ids=sm26963608,sm26963450,sm26963253

            var url = new StringBuilder($"{NiconicoUrls.CommunityV1ApiUrl}videos.json?video_ids=")
                .AppendJoin(Uri.EscapeDataString(","), videoIds)
                .ToString();

            return _context.GetJsonAsAsync<CommunityVideoListItemsResponse>(url, _options);
        }

        public Task<CommunityLiveResponse> GetCommunityLiveAsync(string communityId, int? offset = 0, int? limit = 30)
        {
            // ex) https://com.nicovideo.jp/api/v1/communities/1408350/lives.json?limit=30&offset=0

            string nonPrefixCommunityId = ContentIdHelper.EnsureNonPrefixCommunityId(communityId);
            NameValueCollection dict = new();
            dict.AddIfNotNull("offset", offset);
            dict.AddIfNotNull("limit", limit);

            var url = new StringBuilder(Urls.CommunityV1CommunitiesApiUrl)
                .Append(nonPrefixCommunityId)
                .Append("/lives.json")
                .AppendQueryString(dict)
                .ToString();
            return _context.GetJsonAsAsync<CommunityLiveResponse>(url, _options);
        }
    }

   
}
