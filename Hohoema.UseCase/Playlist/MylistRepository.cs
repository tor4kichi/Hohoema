using Hohoema.Models.Niconico;
using Hohoema.Models.Repository.Niconico.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Playlist
{
    public class MylistRepository
    {
        private readonly NiconicoSession _niconicoSession;
        private readonly UserMylistManager _userMylistManager;
        private readonly OtherOwneredMylistManager _otherOwneredMylistManager;

        public MylistRepository(
            NiconicoSession niconicoSession,
            UserMylistManager userMylistManager,
            OtherOwneredMylistManager otherOwneredMylistManager
            )
        {
            _niconicoSession = niconicoSession;
            _userMylistManager = userMylistManager;
            _otherOwneredMylistManager = otherOwneredMylistManager;
        }

        public const string DefailtMylistId = "0";

        public bool IsLoginUserMylistId(string mylistId)
        {
            return _userMylistManager.HasMylistGroup(mylistId);
        }


        public async Task<MylistPlaylist> GetMylist(string mylistId)
        {
            if (_userMylistManager.HasMylistGroup(mylistId))
            {
                return _userMylistManager.GetMylistGroup(mylistId);
            }
            else
            {
                return await _otherOwneredMylistManager.GetMylist(mylistId);
            }
        }

        public async Task<List<MylistPlaylist>> GetUserMylistsAsync(string userId)
        {
            if (_niconicoSession.UserIdString == userId)
            {
                return _userMylistManager.Mylists.Cast<MylistPlaylist>().ToList();
            }
            else
            {
                return await _otherOwneredMylistManager.GetByUserId(userId);
            }
        }
    }
}
