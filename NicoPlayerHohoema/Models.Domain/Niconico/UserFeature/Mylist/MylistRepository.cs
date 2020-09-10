using Mntone.Nico2.Mylist;

using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Video;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiconicoSession = Hohoema.Models.Domain.NiconicoSession;

namespace Hohoema.Models.Domain.Niconico.UserFeature.Mylist
{
    public class MylistItemsGetResult
    {
        public bool IsSuccess { get; set; }
        public IMylist Mylist { get; set; }
        public bool IsLoginUserMylist { get; set; }
        public bool IsDefaultMylist { get; set; }
        public int ItemsHeadPosition { get; set; }
        public int TotalCount { get; set; }
        public IReadOnlyCollection<IVideoContent> Items { get; set; }

        public int Count => Items.Count;
    }



    public static class MylistPlaylistExtension
    {
        public const string DefailtMylistId = "0";

        public static bool IsDefaultMylist(this IMylist mylist)
        {
            return mylist?.Id == DefailtMylistId;
        }
    }

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
