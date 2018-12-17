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
            Models.HohoemaPlaylist hohoemaPlaylist
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
        public Models.HohoemaPlaylist HohoemaPlaylist { get; }

        public async Task<Interfaces.IMylist> FindMylist(string id, Models.PlaylistOrigin? origin = null)
        {
            if (!origin.HasValue)
            {
                if (UserMylistManager.HasMylistGroup(id))
                {
                    origin = Models.PlaylistOrigin.LoginUser;
                }
                else if (Models.HohoemaPlaylist.WatchAfterPlaylistId == id)
                {
                    origin = Models.PlaylistOrigin.Local;
                }
                else if (LocalMylistManager.LocalMylistGroups.FirstOrDefault(x => x.Id == id) != null)
                {
                    origin = Models.PlaylistOrigin.Local;
                }
                else
                {
                    origin = Models.PlaylistOrigin.OtherUser;
                }
            }

            switch (origin.Value)
            {
                case Models.PlaylistOrigin.LoginUser:
                    // ログインユーザーのマイリスト
                    return UserMylistManager.GetMylistGroup(id);
                case Models.PlaylistOrigin.Local:
                    // ローカルマイリスト
                    if (Models.HohoemaPlaylist.WatchAfterPlaylistId == id)
                    {
                        return HohoemaPlaylist.DefaultPlaylist;
                    }
                    else
                    {
                        return LocalMylistManager.LocalMylistGroups.FirstOrDefault(x => x.Id == id);
                    }
                case Models.PlaylistOrigin.OtherUser:
                    // 他ユーザーのマイリスト
                    return await OtherOwneredMylistManager.GetMylist(id);
                default:
                    throw new Exception("not found mylist:" + id);
            }

        }

        public Interfaces.IMylist FindMylistInCached(string id, Models.PlaylistOrigin? origin = null)
        {
            if (!origin.HasValue)
            {
                if (UserMylistManager.HasMylistGroup(id))
                {
                    origin = Models.PlaylistOrigin.LoginUser;
                }
                else if (Models.HohoemaPlaylist.WatchAfterPlaylistId == id)
                {
                    origin = Models.PlaylistOrigin.Local;
                }
                else if (LocalMylistManager.LocalMylistGroups.FirstOrDefault(x => x.Id == id) != null)
                {
                    origin = Models.PlaylistOrigin.Local;
                }
                else
                {
                    origin = Models.PlaylistOrigin.OtherUser;
                }
            }

            switch (origin.Value)
            {
                case Models.PlaylistOrigin.LoginUser:
                    // ログインユーザーのマイリスト
                    return UserMylistManager.GetMylistGroup(id);
                case Models.PlaylistOrigin.Local:
                    // ローカルマイリスト
                    if (Models.HohoemaPlaylist.WatchAfterPlaylistId == id)
                    {
                        return HohoemaPlaylist.DefaultPlaylist;
                    }
                    else
                    {
                        return LocalMylistManager.LocalMylistGroups.FirstOrDefault(x => x.Id == id);
                    }
                case Models.PlaylistOrigin.OtherUser:
                    // 他ユーザーのマイリスト
                    return OtherOwneredMylistManager.GetMylistIfCached(id);
                default:
                    throw new Exception("not found mylist:" + id);
            }

        }
    }
}
