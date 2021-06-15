using System.ComponentModel;

namespace NiconicoToolkit.Channels
{
    public enum ChannelVideoSortKey
    {
        [Description("f")]
        FirstRetrieve,

        [Description("v")]
        ViewCount,

        [Description("r")]
        CommentCount,

        [Description("m")]
        MylistCount,

        [Description("n")]
        NewComment,

        [Description("l")]
        Length,
    }
}
