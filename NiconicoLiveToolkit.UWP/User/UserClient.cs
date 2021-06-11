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

        internal UserClient(NiconicoContext context)
        {
            _context = context;
            _options = new JsonSerializerOptions()
            {
                Converters =
                {
                    new JsonStringEnumMemberConverter()
                }
            };
        }


        public static class Urls
        {
            public static string MakeUsersApiUrl(IEnumerable<int> userIds)
            {
                return new StringBuilder("https://public.api.nicovideo.jp/v1/users.json?userIds=")
                    .AppendJoin(Uri.EscapeDataString(","), userIds)
                    .ToString();
            }
        }


        const string NicknameApiUrlFormat = "https://api.live2.nicovideo.jp/api/v1/user/nickname?userId={0}";
        public async Task<UserNickname> GetUserNicknameAsync(string id)
        {
            var res = await _context.GetJsonAsAsync<UserNicknameResponse>(string.Format(NicknameApiUrlFormat, id));
            return res.User;
        }

        public async Task<UserNickname> GetUserNicknameAsync(uint id)
        {
            var res = await _context.GetJsonAsAsync<UserNicknameResponse>(string.Format(NicknameApiUrlFormat, id));
            return res.User;
        }



        const string UserDetailsApiUrlFormat = "http://api.ce.nicovideo.jp/api/v1/user.info?__format=json&user_id={0}";
        public async Task<NicovideoUserResponse> GetUserInfoAsync(string id)
        {
            var res = await _context.GetJsonAsAsync<NicovideoUserResponseContainer>(string.Format(UserDetailsApiUrlFormat, id));
            return res.Response;
        }

        public async Task<NicovideoUserResponse> GetUserInfoAsync(uint id)
        {   
            var res = await _context.GetJsonAsAsync<NicovideoUserResponseContainer>(string.Format(UserDetailsApiUrlFormat, id));
            return res.Response;
        }



        public static string MakeUserPageUrl<IdType>(IdType userId)
        {
            return $"https://www.nicovideo.jp/user/{userId}";
        }


        public Task<UserDetailResponse> GetUserDetailAsync(string userId)
        {
            return GetUserDetailAsync_Internal(userId);
        }

        public Task<UserDetailResponse> GetUserDetailAsync(uint userId)
        {
            return GetUserDetailAsync_Internal(userId);
        }

        private async Task<UserDetailResponse> GetUserDetailAsync_Internal<IdType>(IdType userId)
        {
            await _context.WaitPageAccessAsync();

            var res = await _context.GetAsync(MakeUserPageUrl(userId));
            if (!res.IsSuccessStatusCode)
            {
                return new UserDetailResponse()
                {
                    Meta = new Meta() { Status = (long)res.StatusCode }
                };
            }

            HtmlParser parser = new HtmlParser();
            using (var stream = await res.Content.ReadAsInputStreamAsync())
            using (var document = await parser.ParseDocumentAsync(stream.AsStreamForRead()))
            {
                var dataNode = document.QuerySelector("#js-initial-userpage-data");
                var json = dataNode.GetAttribute("data-initial-data");
                var userDetailRes = JsonSerializer.Deserialize<UserDetailResponseContainer>(WebUtility.HtmlDecode(json), _options);
                userDetailRes.Detail.Meta = new Meta()
                {
                    Status = (long)res.StatusCode
                };
                return userDetailRes.Detail;
            }
        }




        public Task<UserVideoResponse> GetUserVideoAsync(uint userId, int page = 0, int pageSize = 100, UserVideoSortKey sortKey = UserVideoSortKey.RegisteredAt, UserVideoSortOrder sortOrder = UserVideoSortOrder.Desc)
        {
            return GetUserVideoAsync_Internal(userId, page, pageSize, sortKey, sortOrder);
        }

        public Task<UserVideoResponse> GetUserVideoAsync(string userId, int page = 0, int pageSize = 100, UserVideoSortKey sortKey = UserVideoSortKey.RegisteredAt, UserVideoSortOrder sortOrder = UserVideoSortOrder.Desc)
        {
            return GetUserVideoAsync_Internal(userId, page, pageSize, sortKey, sortOrder);
        }

        private Task<UserVideoResponse> GetUserVideoAsync_Internal<IdType>(IdType userId, int page = 0, int pageSize = 100, UserVideoSortKey sortKey = UserVideoSortKey.RegisteredAt, UserVideoSortOrder sortOrder = UserVideoSortOrder.Desc)
        {
            return _context.GetJsonAsAsync<UserVideoResponse>(
                $"https://nvapi.nicovideo.jp/v2/users/{userId}/videos?sortKey={sortKey.GetDescription()}&sortOrder={sortOrder.GetDescription()}&pageSize={pageSize}&page={page + 1}"
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
            var url = new StringBuilder("https://public.api.nicovideo.jp/v1/users.json")
                .AppendQueryString(dict)
                .ToString();

            return _context.GetJsonAsAsync<UsersResponse>(url);
        }

    }

    public sealed class UsersResponse : ResponseWithMeta
    {
        [JsonPropertyName("data")]
        public UsersData[] Data { get; set; }


        public sealed class UsersData
        {
            [JsonPropertyName("userId")]
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            public int UserId { get; set; }

            [JsonPropertyName("nickname")]
            public string Nickname { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("hasPremiumOrStrongerRights")]
            public bool HasPremiumOrStrongerRights { get; set; }

            [JsonPropertyName("hasSuperPremiumOrStrongerRights")]
            public bool HasSuperPremiumOrStrongerRights { get; set; }

            [JsonPropertyName("icons")]
            public Icons Icons { get; set; }
        }

        public sealed class Icons
        {
            [JsonPropertyName("urls")]
            public Urls Urls { get; set; }
        }

        public sealed class Urls
        {
            [JsonPropertyName("150x150")]
            public Uri The150X150 { get; set; }

            [JsonPropertyName("50x50")]
            public Uri The50X50 { get; set; }
        }
    }

}
