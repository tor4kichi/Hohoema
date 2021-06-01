using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.Mylist.LoginUser;
using Hohoema.Presentation.Services;
using I18NPortable;
using NiconicoToolkit.Mylist;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Threading;

namespace Hohoema.Models.UseCase.NicoVideos
{



    // TODO: アイテム個数上限による失敗


    public class LoginUserOwnedMylistManager : BindableBase
    {
        public LoginUserOwnedMylistManager(
            NiconicoSession niconicoSession,
            LoginUserMylistProvider loginUserMylistProvider,
            NotificationService notificationService
            )
        {
            _niconicoSession = niconicoSession;
            _loginUserMylistProvider = loginUserMylistProvider;
            _notificationService = notificationService;
            _mylists = new ObservableCollection<LoginUserMylistPlaylist>();
            Mylists = new ReadOnlyObservableCollection<LoginUserMylistPlaylist>(_mylists);

            _niconicoSession.LogIn += async (_, e) =>
            {
                using (await _mylistSyncLock.LockAsync(default))
                {
                    IsLoginUserMylistReady = false;

                    await SyncMylistGroups();

                    IsLoginUserMylistReady = true;
                }
            };

            _niconicoSession.LogOut += async (_, e) =>
            {
                using (await _mylistSyncLock.LockAsync(default))
                {
                    IsLoginUserMylistReady = false;

                    _mylists.Clear();
                }
            };
        }




        FastAsyncLock _mylistSyncLock = new FastAsyncLock();

        private bool _IsLoginUserMylistReady;
        public bool IsLoginUserMylistReady
        {
            get { return _IsLoginUserMylistReady; }
            set { SetProperty(ref _IsLoginUserMylistReady, value); }
        }


        readonly private NiconicoSession _niconicoSession;
        readonly private LoginUserMylistProvider _loginUserMylistProvider;
        private readonly NotificationService _notificationService;

        public LoginUserMylistPlaylist Deflist { get; private set; }

		private ObservableCollection<LoginUserMylistPlaylist> _mylists;
		public ReadOnlyObservableCollection<LoginUserMylistPlaylist> Mylists { get; private set; }

        private FastAsyncLock _updateLock = new FastAsyncLock();

        

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


        private void HandleMylistItemChanged(LoginUserMylistPlaylist playlist)
        {
            playlist.MylistItemAdded += Playlist_MylistItemAdded;
            playlist.MylistItemRemoved += Playlist_MylistItemRemoved;
        }

        private void RemoveHandleMylistItemChanged(LoginUserMylistPlaylist playlist)
        {
            playlist.MylistItemAdded -= Playlist_MylistItemAdded;
            playlist.MylistItemRemoved -= Playlist_MylistItemRemoved;
        }


        private void Playlist_MylistItemAdded(object sender, MylistItemAddedEventArgs e)
        {
            var playlist = (LoginUserMylistPlaylist)sender;
            if (e.FailedItems?.Any() ?? false)
            {
                _notificationService.ShowLiteInAppNotification_Fail("InAppNotification_MylistAddedItems_Fail".Translate(playlist.Label));
            }
            else
            {
                _notificationService.ShowLiteInAppNotification_Success("InAppNotification_MylistAddedItems_Success".Translate(playlist.Label, e.SuccessedItems.Count));
            }
        }

        private void Playlist_MylistItemRemoved(object sender, MylistItemRemovedEventArgs e)
        {
            var playlist = (LoginUserMylistPlaylist)sender;
            if (e.FailedItems?.Any() ?? false)
            {
                _notificationService.ShowLiteInAppNotification_Fail("InAppNotification_MylistRemovedItems_Fail".Translate(playlist.Label));
            }
            else
            {
                _notificationService.ShowLiteInAppNotification_Success("InAppNotification_MylistRemovedItems_Success".Translate(playlist.Label, e.SuccessedItems.Count));
            }
        }


        public bool HasMylistGroup(string groupId)
		{
			return Mylists.Any(x => x.Id == groupId);
		}

        public async Task WaitUpdate(CancellationToken ct = default)
        {
            using var _ = await _updateLock.LockAsync(ct);
        }

		public LoginUserMylistPlaylist GetMylistGroup(string groupId)
		{
			return Mylists.SingleOrDefault(x => x.Id == groupId);
		}

        public async Task<LoginUserMylistPlaylist> GetMylistGroupAsync(string groupId, CancellationToken ct = default)
        {
            using (await _updateLock.LockAsync(ct))
            {
                return Mylists.SingleOrDefault(x => x.Id == groupId);
            }
        }


        public async Task SyncMylistGroups(CancellationToken ct = default)
		{
            using (var releaser = await _updateLock.LockAsync(ct))            
            {
                Deflist = null;

                _mylists.ForEach(RemoveHandleMylistItemChanged);
                _mylists.Clear();

                if (_niconicoSession.IsLoggedIn)
                {
                    try
                    {
                        var groups = await _loginUserMylistProvider.GetLoginUserMylistGroups();

                        ct.ThrowIfCancellationRequested();

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

                _mylists.ForEach(HandleMylistItemChanged);
            }
		}

		public async Task<LoginUserMylistPlaylist> AddMylist(string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            var result = await _loginUserMylistProvider.AddMylist(name, description, isPublic, sortKey, sortOrder);
            if (result != null)
            {
                await SyncMylistGroups();
                return _mylists.FirstOrDefault(x => x.Id == result);
            }
            else
            {
                return null;
            }
        }

		
		public async Task<bool> RemoveMylist(string mylistGroupId)
		{
			var result = await _loginUserMylistProvider.RemoveMylist(mylistGroupId);

			if (result)
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

    }

	public class MylistVideoItemInfo
	{
		public string VideoId { get; set; }
		public string ThreadId { get; set; }
	}
}
