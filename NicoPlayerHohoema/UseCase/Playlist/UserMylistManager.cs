using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Repository.Playlist;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
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

    public sealed class MylistItemRemovedEventArgs
    {
        public string MylistId { get; internal set; }
        public IReadOnlyCollection<string> SuccessedItems { get; internal set; }
        public IReadOnlyCollection<string> FailedItems { get; internal set; }
    }


    public sealed class MylistItemAddedEventArgs
    {
        public string MylistId { get; internal set; }
        public IReadOnlyCollection<string> SuccessedItems { get; internal set; }
        public IReadOnlyCollection<string> FailedItems { get; internal set; }
    }


    public class UserMylistManager : BindableBase
    {
        public UserMylistManager(
            NiconicoSession niconicoSession,
            Provider.LoginUserMylistProvider loginUserMylistProvider
            )
        {
            _niconicoSession = niconicoSession;
            _loginUserMylistProvider = loginUserMylistProvider;

            _mylists = new ObservableCollection<LoginUserMylistPlaylist>();
            Mylists = new ReadOnlyObservableCollection<LoginUserMylistPlaylist>(_mylists);

            _niconicoSession.LogIn += async (_, e) =>
            {
                using (await _mylistSyncLock.LockAsync())
                {
                    IsLoginUserMylistReady = false;

                    await SyncMylistGroups();

                    IsLoginUserMylistReady = true;
                }
            };

            _niconicoSession.LogOut += async (_, e) =>
            {
                using (await _mylistSyncLock.LockAsync())
                {
                    IsLoginUserMylistReady = false;

                    _mylists.Clear();
                }
            };
        }

        public event EventHandler<MylistItemAddedEventArgs> MylistItemAdded;
        public event EventHandler<MylistItemRemovedEventArgs> MylistItemRemoved;




        AsyncLock _mylistSyncLock = new AsyncLock();

        private bool _IsLoginUserMylistReady;
        public bool IsLoginUserMylistReady
        {
            get { return _IsLoginUserMylistReady; }
            set { SetProperty(ref _IsLoginUserMylistReady, value); }
        }


        readonly private NiconicoSession _niconicoSession;
        readonly private Provider.LoginUserMylistProvider _loginUserMylistProvider;
        public LoginUserMylistPlaylist Deflist { get; private set; }

		private ObservableCollection<LoginUserMylistPlaylist> _mylists;
		public ReadOnlyObservableCollection<LoginUserMylistPlaylist> Mylists { get; private set; }

        private AsyncLock _updateLock = new AsyncLock();

        

        public int DeflistRegistrationCapacity => _niconicoSession.IsPremiumAccount ? 500 : 100;

        public int DeflistRegistrationCount => Deflist.Count;
        public int MylistRegistrationCapacity => _niconicoSession.IsPremiumAccount ? 25000 : 100;

        public int MylistRegistrationCount => Mylists.Where(x => !x.IsDefaultMylist()).Sum((System.Func<MylistPlaylist, int>)(x => (int)x.Count));

        public const int MaxUserMylistGroupCount = 25;
        public const int MaxPremiumUserMylistGroupCount = 50;

        public int MaxMylistGroupCountCurrentUser => 
            _niconicoSession.IsPremiumAccount ? MaxPremiumUserMylistGroupCount : MaxUserMylistGroupCount;



        public bool CanAddMylistGroup => Mylists.Count < MaxMylistGroupCountCurrentUser;


        public bool IsDeflistCapacityReached => DeflistRegistrationCount >= DeflistRegistrationCapacity;

        public bool CanAddMylistItem => MylistRegistrationCount < MylistRegistrationCapacity;


      
        

		public bool HasMylistGroup(string groupId)
		{
			return Mylists.Any(x => x.Id == groupId);
		}


		public LoginUserMylistPlaylist GetMylistGroup(string groupId)
		{
			return Mylists.SingleOrDefault(x => x.Id == groupId);
		}


		public async Task SyncMylistGroups()
		{
            using (var releaser = await _updateLock.LockAsync())
            {
                Deflist = null;
                _mylists.Clear();

                await Task.Delay(TimeSpan.FromSeconds(2));

                if (_niconicoSession.IsLoggedIn)
                {
                    try
                    {
                        var groups = await _loginUserMylistProvider.GetLoginUserMylistGroups();
                        foreach (var mylistGroup in groups ?? Enumerable.Empty<LoginUserMylistPlaylist>())
                        {
                            if (mylistGroup.IsDefaultMylist())
                            {
                                Deflist = mylistGroup;
                            }

                            _mylists.Add(mylistGroup);
                        }
                    }
                    catch
                    {
                        _mylists.Clear();
                    }
                }
            }
		}

		public async Task<ContentManageResult> AddMylist(string name, string description, bool isPublic, MylistDefaultSort default_sort, IconType iconType)
		{
            var result = await _loginUserMylistProvider.AddMylist(name, description, isPublic, default_sort, iconType);

            if (result == ContentManageResult.Success)
            {
                await SyncMylistGroups();
            }

            return result;
		}

		
		public async Task<ContentManageResult> RemoveMylist(string mylistGroupId)
		{
			var result = await _loginUserMylistProvider.RemoveMylist(mylistGroupId);

			if (result == ContentManageResult.Success)
			{
                await SyncMylistGroups();
            }

			return result;
		}


        public bool CheckIsRegistratedAnyMylist(string videoId)
		{
            throw new NotImplementedException();
			//return Mylists.Any(x => x.ContainsVideoId(videoId));
		}

        public Task<List<IVideoContent>> GetLoginUserMylistItemsAsync(IMylist mylist)
        {
            return _loginUserMylistProvider.GetLoginUserMylistItemsAsync(mylist);
        }


        public Task<ContentManageResult> UpdateMylist(string mylistId, Dialogs.MylistGroupEditData editData)
        {
            return _loginUserMylistProvider.UpdateMylist(mylistId, editData);
        }




        public Task<MylistItemAddedEventArgs> AddItem(string mylistId, string videoId, string mylistComment = "")
        {
            return AddItem(mylistId, new[] { videoId }, mylistComment);
        }

        public async Task<MylistItemAddedEventArgs> AddItem(string mylistId, IEnumerable<string> items, string mylistComment = "")
        {
            List<string> successed = new List<string>();
            List<string> failed = new List<string>();

            foreach (var videoId in items)
            {
                var result = await  _loginUserMylistProvider.AddMylistItem(mylistId, videoId, mylistComment);
                if (result != ContentManageResult.Failed)
                {
                    successed.Add(videoId);
                }
                else
                {
                    failed.Add(videoId);
                }
            }

            var args = new MylistItemAddedEventArgs()
            {
                MylistId = mylistId,
                SuccessedItems = successed,
                FailedItems = failed
            };
            MylistItemAdded?.Invoke(this, args);

            return args;
        }


        public Task<MylistItemRemovedEventArgs> RemoveItem(string mylistId, string videoId)
        {
            return RemoveItem(mylistId, new[] { videoId });
        }

        public async Task<MylistItemRemovedEventArgs> RemoveItem(string mylistId, IEnumerable<string> items)
        {
            List<string> successed = new List<string>();
            List<string> failed = new List<string>();

            foreach (var videoId in items)
            {
                var result = await _loginUserMylistProvider.RemoveMylistItem(mylistId, videoId);
                if (result == ContentManageResult.Success)
                {
                    successed.Add(videoId);
                }
                else
                {
                    failed.Add(videoId);
                }
            }

            var args = new MylistItemRemovedEventArgs()
            {
                MylistId = mylistId,
                SuccessedItems = successed,
                FailedItems = failed
            };
            MylistItemRemoved?.Invoke(this, args);

            return args;
        }
    }

	public class MylistVideoItemInfo
	{
		public string VideoId { get; set; }
		public string ThreadId { get; set; }
	}
}
