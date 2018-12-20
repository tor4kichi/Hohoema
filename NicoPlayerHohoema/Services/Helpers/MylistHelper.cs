using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Services.Helpers
{
    public sealed class MylistHelper
    {
        public MylistHelper(
            Models.UserMylistManager userMylistManager,
            Models.OtherOwneredMylistManager otherOwneredMylistManager,
            Models.LocalMylist.LocalMylistManager localMylistManager,
            Services.HohoemaPlaylist hohoemaPlaylist
            )
        {
            UserMylistManager = userMylistManager;
            OtherOwneredMylistManager = otherOwneredMylistManager;
            LocalMylistManager = localMylistManager;
            HohoemaPlaylist = hohoemaPlaylist;
        }

        public Models.UserMylistManager UserMylistManager { get; }
        public Models.OtherOwneredMylistManager OtherOwneredMylistManager { get; }
        public Models.LocalMylist.LocalMylistManager LocalMylistManager { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }

        public async Task<Interfaces.IMylist> FindMylist(string id, Services.PlaylistOrigin? origin = null)
        {
            if (!origin.HasValue)
            {
                if (UserMylistManager.HasMylistGroup(id))
                {
                    origin = Services.PlaylistOrigin.LoginUser;
                }
                else if (Services.HohoemaPlaylist.WatchAfterPlaylistId == id)
                {
                    origin = Services.PlaylistOrigin.Local;
                }
                else if (LocalMylistManager.Mylists.FirstOrDefault(x => x.Id == id) != null)
                {
                    origin = Services.PlaylistOrigin.Local;
                }
                else
                {
                    origin = Services.PlaylistOrigin.OtherUser;
                }
            }

            switch (origin.Value)
            {
                case Services.PlaylistOrigin.LoginUser:
                    // ログインユーザーのマイリスト
                    return UserMylistManager.GetMylistGroup(id);
                case Services.PlaylistOrigin.Local:
                    // ローカルマイリスト
                    if (Services.HohoemaPlaylist.WatchAfterPlaylistId == id)
                    {
                        return HohoemaPlaylist.DefaultPlaylist;
                    }
                    else
                    {
                        return LocalMylistManager.Mylists.FirstOrDefault(x => x.Id == id);
                    }
                case Services.PlaylistOrigin.OtherUser:
                    // 他ユーザーのマイリスト
                    return await OtherOwneredMylistManager.GetMylist(id);
                default:
                    throw new Exception("not found mylist:" + id);
            }

        }

        public Interfaces.IMylist FindMylistInCached(string id, Services.PlaylistOrigin? origin = null)
        {
            if (!origin.HasValue)
            {
                if (UserMylistManager.HasMylistGroup(id))
                {
                    origin = Services.PlaylistOrigin.LoginUser;
                }
                else if (Services.HohoemaPlaylist.WatchAfterPlaylistId == id)
                {
                    origin = Services.PlaylistOrigin.Local;
                }
                else if (LocalMylistManager.Mylists.FirstOrDefault(x => x.Id == id) != null)
                {
                    origin = Services.PlaylistOrigin.Local;
                }
                else
                {
                    origin = Services.PlaylistOrigin.OtherUser;
                }
            }

            switch (origin.Value)
            {
                case Services.PlaylistOrigin.LoginUser:
                    // ログインユーザーのマイリスト
                    return UserMylistManager.GetMylistGroup(id);
                case Services.PlaylistOrigin.Local:
                    // ローカルマイリスト
                    if (Services.HohoemaPlaylist.WatchAfterPlaylistId == id)
                    {
                        return HohoemaPlaylist.DefaultPlaylist;
                    }
                    else
                    {
                        return LocalMylistManager.Mylists.FirstOrDefault(x => x.Id == id);
                    }
                case Services.PlaylistOrigin.OtherUser:
                    // 他ユーザーのマイリスト
                    return OtherOwneredMylistManager.GetMylistIfCached(id);
                default:
                    throw new Exception("not found mylist:" + id);
            }

        }
    }
}
