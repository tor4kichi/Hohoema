using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    public static class MylistHohoemaAppExtentions
    {
        public static async Task<IPlayableList> GetPlayableList(this HohoemaApp hohoemaApp, string id, PlaylistOrigin? origin)
        {
            if (!origin.HasValue)
            {
                if (hohoemaApp.UserMylistManager.HasMylistGroup(id))
                {
                    origin = PlaylistOrigin.LoginUser;
                }
                else if (HohoemaPlaylist.WatchAfterPlaylistId == id)
                {
                    origin = PlaylistOrigin.Local;
                }
                else if (hohoemaApp.Playlist.Playlists.FirstOrDefault(x => x.Id == id) != null)
                {
                    origin = PlaylistOrigin.Local;
                }
                else
                {
                    origin = PlaylistOrigin.OtherUser;
                }
            }

            switch (origin.Value)
            {
                case PlaylistOrigin.LoginUser:
                    // ログインユーザーのマイリスト
                    return hohoemaApp.UserMylistManager.GetMylistGroup(id);
                case PlaylistOrigin.Local:
                    // ローカルマイリスト
                    if (id == HohoemaPlaylist.WatchAfterPlaylistId)
                    {
                        return hohoemaApp.Playlist.DefaultPlaylist;
                    }
                    else
                    {
                        return hohoemaApp.Playlist.Playlists.FirstOrDefault(x => x.Id == id);
                    }
                case PlaylistOrigin.OtherUser:
                    // 他ユーザーのマイリスト
                    return await hohoemaApp.OtherOwneredMylistManager.GetMylist(id);
                default:
                    throw new Exception("not found mylist:" + id);
            }
            
        }
    }
}
