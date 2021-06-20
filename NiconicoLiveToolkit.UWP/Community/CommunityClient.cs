using NiconicoToolkit.User;
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

        internal CommunityClient(NiconicoContext context, JsonSerializerOptions defaultOptions)
        {
            _context = context;
            _options = new JsonSerializerOptions(defaultOptions) 
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


        public async Task<CommunityInfoResponse> GetCommunityInfoAsync(CommunityId communityId)
        {
            var res = await _context.GetJsonAsAsync<CommunityInfoResponseContainer>($"{NiconicoUrls.CeApiV1Url}community.info?id={communityId}", _options);
            return res?.Response;
        }

        public Task<CommunityAuthorityResponse> GetCommunityAuthorityForLoginUserAsync(CommunityId communityId)
        {
            var url = new StringBuilder(Urls.CommunityV1CommunitiesApiUrl)
                .Append(communityId.ToStringWithoutPrefix())
                .Append("/authority.json")
                .ToString();

            return _context.GetJsonAsAsync<CommunityAuthorityResponse>(url, _options);
        }

        public Task<CommunityVideoResponse> GetCommunityVideoListAsync(CommunityId communityId, int? offset = 0, int? limit = 20, CommunityVideoSortKey? sortKey = null, CommunityVideoSortOrder? sortOrder = null)
        {
            // ex) https://com.nicovideo.jp/api/v1/communities/540200/contents/videos.json?limit=20&offset=0&sort=c&direction=d&_=1623371150520

            NameValueCollection dict = new();
            dict.AddIfNotNull("offset", offset);
            dict.AddIfNotNull("limit", limit);
            dict.AddEnumIfNotNullWithDescription("sort", sortKey);
            dict.AddEnumIfNotNullWithDescription("direction", sortOrder);

            var url = new StringBuilder(Urls.CommunityV1CommunitiesApiUrl)
                .Append(communityId.ToStringWithoutPrefix())
                .Append("/contents/videos.json")
                .AppendQueryString(dict)
                .ToString();

            return _context.GetJsonAsAsync<CommunityVideoResponse>(url, _options);
        }

        public Task<CommunityVideoListItemsResponse> GetCommunityVideoListItemsAsync(IEnumerable<NiconicoId> videoIds)
        {
            // ex) https://com.nicovideo.jp/api/v1/videos.json?video_ids=sm26963608,sm26963450,sm26963253

            var url = new StringBuilder($"{NiconicoUrls.CommunityV1ApiUrl}videos.json?video_ids=")
                .AppendJoin(Uri.EscapeDataString(","), videoIds)
                .ToString();

            return _context.GetJsonAsAsync<CommunityVideoListItemsResponse>(url, _options);
        }

        public Task<CommunityLiveResponse> GetCommunityLiveAsync(CommunityId communityId, int? offset = 0, int? limit = 30)
        {
            // ex) https://com.nicovideo.jp/api/v1/communities/1408350/lives.json?limit=30&offset=0

            NameValueCollection dict = new();
            dict.AddIfNotNull("offset", offset);
            dict.AddIfNotNull("limit", limit);

            var url = new StringBuilder(Urls.CommunityV1CommunitiesApiUrl)
                .Append(communityId.ToStringWithoutPrefix())
                .Append("/lives.json")
                .AppendQueryString(dict)
                .ToString();
            return _context.GetJsonAsAsync<CommunityLiveResponse>(url, _options);
        }
    }


}
