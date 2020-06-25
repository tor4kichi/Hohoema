using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Repository.Playlist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Playlist
{
    public class PlaylistAggregateGetter
    {
        private readonly MylistRepository _mylistRepository;
        private readonly LocalMylistManager _localMylistManager;
        private readonly HohoemaPlaylist _hohoemaPlaylist;

        public PlaylistAggregateGetter(
            MylistRepository mylistRepository,
            LocalMylistManager localMylistManager,
            HohoemaPlaylist hohoemaPlaylist
            )
        {
            _mylistRepository = mylistRepository;
            _localMylistManager = localMylistManager;
            _hohoemaPlaylist = hohoemaPlaylist;
        }


        public async Task<Interfaces.IPlaylist> FindPlaylistAsync(string id)
        {
            if (HohoemaPlaylist.WatchAfterPlaylistId == id)
            {
                return _hohoemaPlaylist.WatchAfterPlaylist;
            }
            else if (_localMylistManager.HasPlaylist(id))
            {
                return _localMylistManager.GetPlaylist(id);
            }
            else 
            {
                return await _mylistRepository.GetMylist(id);
            }
        }
    }
}
