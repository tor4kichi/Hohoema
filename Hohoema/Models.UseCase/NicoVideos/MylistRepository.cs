using Mntone.Nico2.Mylist;

using Hohoema.Models.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiconicoSession = Hohoema.Models.Domain.NiconicoSession;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;

namespace Hohoema.Models.UseCase.NicoVideos
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
            await _userMylistManager.WaitUpdate();

            if (_userMylistManager.HasMylistGroup(mylistId))
            {
                return await _userMylistManager.GetMylistGroupAsync(mylistId);
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
