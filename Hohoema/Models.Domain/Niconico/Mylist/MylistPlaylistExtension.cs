using Hohoema.Models.Domain.Niconico.Mylist.LoginUser;

namespace Hohoema.Models.Domain.Niconico.Mylist
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
