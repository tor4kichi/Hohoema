using System.ComponentModel;

namespace NiconicoToolkit.Community
{
    public enum CommunityVideoSortKey
    {
        [Description("c")]
        RegisteredAt,

        [Description("f")]
        FirstRetrieve,

        [Description("v")]
        ViewCount,

        [Description("n")]
        NewComment,

        [Description("r")]
        CommentCount,

        [Description("m")]
        MylistCount,

        [Description("l")]
        Length,

    }

    
}
