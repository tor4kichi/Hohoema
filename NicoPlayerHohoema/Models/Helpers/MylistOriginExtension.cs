using NicoPlayerHohoema.Interfaces;

namespace NicoPlayerHohoema.Models.Helpers
{
    public static class MylistExtension
    {
        public static Models.PlaylistOrigin? ToMylistOrigin(this IMylist mylist)
        {
            switch (mylist)
            {
                case ILocalMylist _: return Models.PlaylistOrigin.Local;
                case IUserOwnedRemoteMylist _: return Models.PlaylistOrigin.LoginUser;
                case IOtherOwnedMylist _: return Models.PlaylistOrigin.OtherUser;
                default: return default(Models.PlaylistOrigin?);
            }
        }
    }
    
}
