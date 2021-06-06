using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
        public async Task<UserInfo> GetUserInfoAsync(string id)
        {
            var res = await _context.GetJsonAsAsync<NicovideoUserResponseContainer>(string.Format(UserDetailsApiUrlFormat, id));
            return res.Response?.User;
        }

        public async Task<UserInfo> GetUserInfoAsync(uint id)
        {   
            var res = await _context.GetJsonAsAsync<NicovideoUserResponseContainer>(string.Format(UserDetailsApiUrlFormat, id));
            return res.Response?.User;
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
    }
}
