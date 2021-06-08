using Mntone.Nico2.Mylist;

using Hohoema.Models.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiconicoSession = Hohoema.Models.Domain.Niconico.NiconicoSession;
using Hohoema.Models.Domain.Niconico.Mylist.LoginUser;
using Hohoema.Models.Domain.Niconico.Mylist;

namespace Hohoema.Models.UseCase.NicoVideos
{

    public class MylistRepository
    {
        private readonly NiconicoSession _niconicoSession;
        private readonly LoginUserOwnedMylistManager _userMylistManager;
        private readonly MylistProvider _mylistProvider;

        public MylistRepository(
            NiconicoSession niconicoSession,
            LoginUserOwnedMylistManager userMylistManager,
            MylistProvider mylistProvider
            )
        {
            _niconicoSession = niconicoSession;
            _userMylistManager = userMylistManager;
            _mylistProvider = mylistProvider;
        }

        public const string DefailtMylistId = "0";

        public bool IsLoginUserMylistId(string mylistId)
        {
            return _userMylistManager.HasMylistGroup(mylistId);
        }


        public async Task<MylistPlaylist> GetMylist(string mylistId)
        {
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

        public async Task<List<MylistPlaylist>> GetUserMylistsAsync(string userId)
        {
            if (_niconicoSession.UserIdString == userId)
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
