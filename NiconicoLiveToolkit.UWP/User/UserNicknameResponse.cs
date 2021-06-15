using System.Text.Json.Serialization;

namespace NiconicoToolkit.User
{
    public class UserNicknameResponse
    {
        [JsonPropertyName("data")]
        public UserNickname User { get; set; }
    }


    public class UserNickname
    {
        [JsonPropertyName("id")]
        public UserId Id { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }
    }
}
