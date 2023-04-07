using Hohoema.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;
using Hohoema.Models.Niconico.Mylist.LoginUser;
using Hohoema.Models.Niconico.Mylist;
using NiconicoToolkit;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.User;

namespace Hohoema.Services.Playlist
{

    public class MylistResolver
    {
        private readonly NiconicoSession _niconicoSession;
        private readonly LoginUserOwnedMylistManager _userMylistManager;
        private readonly MylistProvider _mylistProvider;

        public MylistResolver(
            NiconicoSession niconicoSession,
            LoginUserOwnedMylistManager userMylistManager,
            MylistProvider mylistProvider
            )
        {
            _niconicoSession = niconicoSession;
            _userMylistManager = userMylistManager;
            _mylistProvider = mylistProvider;
        }

        public bool IsLoginUserMylistId(MylistId mylistId)
        {
            return _userMylistManager.HasMylistGroup(mylistId);
        }


        public async Task<MylistPlaylist> GetMylistAsync(MylistId mylistId)
        {
            using var _ = await _niconicoSession.SigninLock.LockAsync();
            await _userMylistManager.WaitUpdate();

            if (_userMylistManager.HasMylistGroup(mylistId))
            {
                return await _userMylistManager.GetMylistGroupAsync(mylistId);
            }
            else
            {
                return await _mylistProvider.GetMylist(mylistId);
            }
        }

        public async Task<List<MylistPlaylist>> GetUserMylistsAsync(UserId userId)
        {
            if (_niconicoSession.UserId == userId)
            {
                return _userMylistManager.Mylists.Cast<MylistPlaylist>().ToList();
            }
            else
            {
                return await _mylistProvider.GetMylistsByUser(userId, 1);
            }
        }
    }
}
