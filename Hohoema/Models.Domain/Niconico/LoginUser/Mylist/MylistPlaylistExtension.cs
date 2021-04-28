using Hohoema.Models.Domain.Niconico.LoginUser.Mylist;

namespace Hohoema.Models.Domain.Niconico.LoginUser.Mylist
{
    public static class MylistPlaylistExtension
    {
        public const string DefailtMylistId = "0";

        public static bool IsDefaultMylist(this IMylist mylist)
        {
            return mylist?.Id == DefailtMylistId;
        }
    }
}
