using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using NicoPlayerHohoema.Models.Helpers;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;

namespace NicoPlayerHohoema.Models
{
    public class UserMylistManager : BindableBase
    {
        public UserMylistManager(
            NiconicoSession niconicoSession,
            Provider.LoginUserMylistProvider loginUserMylistProvider
            )
        {
            NiconicoSession = niconicoSession;
            LoginUserMylistProvider = loginUserMylistProvider;

            _UserMylists = new ObservableCollection<UserOwnedMylist>();
            UserMylists = new ReadOnlyObservableCollection<UserOwnedMylist>(_UserMylists);

            NiconicoSession.LogIn += (_, e) =>
            {
                _ = SyncMylistGroups();
            };
            NiconicoSession.LogOut += (_, e) =>
            {
                _UserMylists.Clear();
            };
        }


        public NiconicoSession NiconicoSession { get; }
        public Provider.LoginUserMylistProvider LoginUserMylistProvider { get; }
        public UserOwnedMylist Deflist { get; private set; }

		private ObservableCollection<UserOwnedMylist> _UserMylists;
		public ReadOnlyObservableCollection<UserOwnedMylist> UserMylists { get; private set; }

        private AsyncLock _UpdateLock = new AsyncLock();

        

        public int DeflistRegistrationCapacity => NiconicoSession.IsPremiumAccount ? 500 : 100;

        public int DeflistRegistrationCount => Deflist.ItemCount;
        public int MylistRegistrationCapacity => NiconicoSession.IsPremiumAccount ? 25000 : 100;

        public int MylistRegistrationCount => UserMylists.Where(x => !x.IsDeflist).Sum((System.Func<UserOwnedMylist, int>)(x => (int)x.ItemCount));

        public const int MaxUserMylistGroupCount = 25;
        public const int MaxPremiumUserMylistGroupCount = 50;

        public int MaxMylistGroupCountCurrentUser => 
            NiconicoSession.IsPremiumAccount ? MaxPremiumUserMylistGroupCount : MaxUserMylistGroupCount;



        public bool CanAddMylistGroup => UserMylists.Count < MaxMylistGroupCountCurrentUser;


        public bool IsDeflistCapacityReached => DeflistRegistrationCount >= DeflistRegistrationCapacity;

        public bool CanAddMylistItem => MylistRegistrationCount < MylistRegistrationCapacity;


      
        

		public bool HasMylistGroup(string groupId)
		{
			return UserMylists.Any(x => x.GroupId == groupId);
		}


		public UserOwnedMylist GetMylistGroup(string groupId)
		{
			return UserMylists.SingleOrDefault(x => x.GroupId == groupId);
		}


		public async Task SyncMylistGroups()
		{
            using (var releaser = await _UpdateLock.LockAsync())
            {
                _UserMylists.Clear();

                var groups = await LoginUserMylistProvider.GetLoginUserMylistGroups();
                foreach (var mylistGroup in groups ?? Enumerable.Empty<UserOwnedMylist>())
                {
                    _UserMylists.Add(mylistGroup);
                }
            }
		}

		public async Task<ContentManageResult> AddMylist(string name, string description, bool isPublic, MylistDefaultSort default_sort, IconType iconType)
		{
            var result = await LoginUserMylistProvider.AddMylist(name, description, isPublic, default_sort, iconType);

            if (result == ContentManageResult.Success)
            {
                await SyncMylistGroups();
            }

            return result;
		}

		
		public async Task<ContentManageResult> RemoveMylist(string mylistGroupId)
		{
			var result = await LoginUserMylistProvider.RemoveMylist(mylistGroupId);

			if (result == ContentManageResult.Success)
			{
                await SyncMylistGroups();
            }

			return result;
		}


        public bool CheckIsRegistratedAnyMylist(string videoId)
		{
			return UserMylists.Any(x => x.ContainsVideoId(videoId));
		}
    }

	public class MylistVideoItemInfo
	{
		public string VideoId { get; set; }
		public string ThreadId { get; set; }
	}
}
