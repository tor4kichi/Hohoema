using System.ComponentModel;

namespace NiconicoToolkit.NicoRepo
{
    public enum NicoRepoDisplayTarget
    {
        [Description("all")]
        All,

        [Description("self")]
        Self,

        [Description("followingUser")]
        User,

        [Description("followingChannel")]
        Channel,

        [Description("followingCommunity")]
        Community,

        [Description("followingMylist")]
        Mylist,
    }

}
