using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using NicoPlayerHohoema.Models.Helpers;
using Prism.Mvvm;
using System;
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

            _Mylists = new ObservableCollection<UserOwnedMylist>();
            Mylists = new ReadOnlyObservableCollection<UserOwnedMylist>(_Mylists);

            NiconicoSession.LogIn += async (_, e) =>
            {
                using (await _mylistSyncLock.LockAsync())
                {
                    IsLoginUserMylistReady = false;

                    await SyncMylistGroups();

                    IsLoginUserMylistReady = true;
                }
            };

            NiconicoSession.LogOut += async (_, e) =>
            {
                using (await _mylistSyncLock.LockAsync())
                {
                    IsLoginUserMylistReady = false;

                    _Mylists.Clear();
                }
            };
        }

        AsyncLock _mylistSyncLock = new AsyncLock();

        private bool _IsLoginUserMylistReady;
        public bool IsLoginUserMylistReady
        {
            get { return _IsLoginUserMylistReady; }
            set { SetProperty(ref _IsLoginUserMylistReady, value); }
        }
       

        public NiconicoSession NiconicoSession { get; }
        public Provider.LoginUserMylistProvider LoginUserMylistProvider { get; }
        public UserOwnedMylist Deflist { get; private set; }

		private ObservableCollection<UserOwnedMylist> _Mylists;
		public ReadOnlyObservableCollection<UserOwnedMylist> Mylists { get; private set; }

        private AsyncLock _UpdateLock = new AsyncLock();

        

        public int DeflistRegistrationCapacity => NiconicoSession.IsPremiumAccount ? 500 : 100;

        public int DeflistRegistrationCount => Deflist.ItemCount;
        public int MylistRegistrationCapacity => NiconicoSession.IsPremiumAccount ? 25000 : 100;

        public int MylistRegistrationCount => Mylists.Where(x => !x.IsDeflist).Sum((System.Func<UserOwnedMylist, int>)(x => (int)x.ItemCount));

        public const int MaxUserMylistGroupCount = 25;
        public const int MaxPremiumUserMylistGroupCount = 50;

        public int MaxMylistGroupCountCurrentUser => 
            NiconicoSession.IsPremiumAccount ? MaxPremiumUserMylistGroupCount : MaxUserMylistGroupCount;



        public bool CanAddMylistGroup => Mylists.Count < MaxMylistGroupCountCurrentUser;


        public bool IsDeflistCapacityReached => DeflistRegistrationCount >= DeflistRegistrationCapacity;

        public bool CanAddMylistItem => MylistRegistrationCount < MylistRegistrationCapacity;


      
        

		public bool HasMylistGroup(string groupId)
		{
			return Mylists.Any(x => x.GroupId == groupId);
		}


		public UserOwnedMylist GetMylistGroup(string groupId)
		{
			return Mylists.SingleOrDefault(x => x.GroupId == groupId);
		}


		public async Task SyncMylistGroups()
		{
            using (var releaser = await _UpdateLock.LockAsync())
            {
                _Mylists.Clear();

                await Task.Delay(TimeSpan.FromSeconds(1));

                if (NiconicoSession.IsLoggedIn)
                {
                    try
                    {
                        var groups = await LoginUserMylistProvider.GetLoginUserMylistGroups();
                        foreach (var mylistGroup in groups ?? Enumerable.Empty<UserOwnedMylist>())
                        {
                            if (mylistGroup.Id == "0")
                            {
                                Deflist = mylistGroup;
                            }

                            _Mylists.Add(mylistGroup);
                        }
                    }
                    catch
                    {
                        _Mylists.Clear();
                    }
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
			return Mylists.Any(x => x.ContainsVideoId(videoId));
		}
    }

	public class MylistVideoItemInfo
	{
		public string VideoId { get; set; }
		public string ThreadId { get; set; }
	}
}
