using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;

namespace Hohoema.Models.Domain.Niconico.UserFeature.Mylist
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
