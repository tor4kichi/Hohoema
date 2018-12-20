using NicoPlayerHohoema.Interfaces;

namespace NicoPlayerHohoema.Models.Helpers
{
    public static class MylistExtension
    {
        public static Services.PlaylistOrigin? ToMylistOrigin(this IMylist mylist)
        {
            switch (mylist)
            {
                case ILocalMylist _: return Services.PlaylistOrigin.Local;
                case IUserOwnedRemoteMylist _: return Services.PlaylistOrigin.LoginUser;
                case IOtherOwnedMylist _: return Services.PlaylistOrigin.OtherUser;
                default: return default(Services.PlaylistOrigin?);
            }
        }
    }
    
}
