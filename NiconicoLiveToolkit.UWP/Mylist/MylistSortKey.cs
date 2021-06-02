using System.ComponentModel;

namespace NiconicoToolkit.Mylist
{
    public enum MylistSortKey
    {
        [Description("title")]
        Title,
        [Description("addedAt")]
        AddedAt,
        [Description("mylistComment")]
        MylistComment,
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
        Duration
    };


}
