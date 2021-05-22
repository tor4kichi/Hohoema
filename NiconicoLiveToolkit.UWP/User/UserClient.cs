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

        internal UserClient(NiconicoContext context)
        {
            _context = context;
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
            return res.NicovideoUserResponse?.User;
        }

        public async Task<UserInfo> GetUserInfoAsync(uint id)
        {   
            var res = await _context.GetJsonAsAsync<NicovideoUserResponseContainer>(string.Format(UserDetailsApiUrlFormat, id));
            return res.NicovideoUserResponse?.User;
        }

        
    }


    public partial class NicovideoUserResponseContainer
    {
        [JsonPropertyName("niconico_response")]
        public NicovideoUserResponse NicovideoUserResponse { get; set; }
    }

    public partial class NicovideoUserResponse
    {
        [JsonPropertyName("user")]
        public UserInfo User { get; set; }

        [JsonPropertyName("vita_option")]
        public VitaOption VitaOption { get; set; }

        [JsonPropertyName("additionals")]
        public string Additionals { get; set; }

        [JsonPropertyName("@status")]
        public string Status { get; set; }

        [JsonPropertyName("error")]
        public Error Error { get; set; }


        public bool IsOK => Status == "ok";
    }

    public partial class Error
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public partial class UserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public Uri ThumbnailUrl { get; set; }
    }

    public partial class VitaOption
    {
        [JsonPropertyName("user_secret")]
        public string UserSecret { get; set; }
    }
    





    public class UserNicknameResponse
    {
        [JsonPropertyName("data")]
        public UserNickname User { get; set; }
    }
    public class UserNickname
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }
    }
}
