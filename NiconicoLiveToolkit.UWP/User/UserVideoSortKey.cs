using System.ComponentModel;

namespace NiconicoToolkit.User
{
    public enum UserVideoSortKey
    {
        [Description("registeredAt")]
        RegisteredAt,

        [Description("viewCount")]
        ViewCount,

        [Description("lastCommentTime")]
        LastCommentTime,

        [Description("commentCount")]
        CommentCount,

        [Description("mylistCount")]
        MylistCount,

        [Description("duration")]
        Duration,
    }
}
