using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Collections.Specialized;

namespace NiconicoToolkit.User
{
    public sealed class UserClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _options;

        internal UserClient(NiconicoContext context, JsonSerializerOptions defaultOptions)
        {
            _context = context;
            _options = defaultOptions;
        }


        internal static class Urls
        {
            public const string CeApiV1UserDetailsApiUrlFormat = $"{NiconicoUrls.CeApiV1Url}user.info?__format=json&user_id={{0}}";
            public const string LiveApiV1NicknameApiUrlFormat = $"{NiconicoUrls.LiveApiV1Url}user/nickname?userId={{0}}";
        }


        public async Task<UserNickname> GetUserNicknameAsync(UserId id)
        {
            var res = await _context.GetJsonAsAsync<UserNicknameResponse>(string.Format(Urls.LiveApiV1NicknameApiUrlFormat, id), _options);
            return res.User;
        }



        public async Task<NicovideoUserResponse> GetUserInfoAsync(UserId id)
        {
            var res = await _context.GetJsonAsAsync<NicovideoUserResponseContainer>(string.Format(Urls.CeApiV1UserDetailsApiUrlFormat, id), _options);
            return res.Response;
        }

              
        public async Task<UserDetailResponse> GetUserDetailAsync(UserId userId)
        {
            await _context.WaitPageAccessAsync();

            using var res = await _context.GetAsync(NiconicoUrls.MakeUserPageUrl(userId));
            if (!res.IsSuccessStatusCode)
            {
                return new UserDetailResponse()
                {
                    Meta = new Meta() { Status = (long)res.StatusCode }
                };
            }

            return await res.Content.ReadHtmlDocumentActionAsync(document =>
            {
                var dataNode = document.QuerySelector("#js-initial-userpage-data");
                var json = dataNode.GetAttribute("data-initial-data");
                var userDetailRes = JsonSerializer.Deserialize<UserDetailResponseContainer>(WebUtility.HtmlDecode(json), _options);
                userDetailRes.Detail.Meta = new Meta()
                {
                    Status = (long)res.StatusCode
                };
                return userDetailRes.Detail;
            });
        }


        public Task<UserVideoResponse> GetUserVideoAsync(UserId userId, int page = 0, int pageSize = 100, UserVideoSortKey sortKey = UserVideoSortKey.RegisteredAt, UserVideoSortOrder sortOrder = UserVideoSortOrder.Desc)
        {
            return _context.GetJsonAsAsync<UserVideoResponse>(
                $"{NiconicoUrls.NvApiV2Url}users/{userId}/videos?sortKey={sortKey.GetDescription()}&sortOrder={sortOrder.GetDescription()}&pageSize={pageSize}&page={page + 1}"
                , _options
                );
        }


        public Task<UsersResponse> GetUsersAsync(IEnumerable<int> userIds)
        {
            var dict = new NameValueCollection();
            foreach (var id in userIds)
            {
                dict.Add("userIds", id.ToString());
            }
            var url = new StringBuilder($"{NiconicoUrls.PublicApiV1Url}users.json")
                .AppendQueryString(dict)
                .ToString();

            return _context.GetJsonAsAsync<UsersResponse>(url, _options);
        }

    }

}
