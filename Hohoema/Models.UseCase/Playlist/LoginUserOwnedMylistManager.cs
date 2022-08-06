using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.Mylist.LoginUser;
using Hohoema.Presentation.Services;
using I18NPortable;
using Microsoft.Extensions.Logging;
using NiconicoToolkit.Mylist;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZLogger;
using static Hohoema.Models.Domain.Niconico.Mylist.LoginUser.LoginUserMylistProvider;
using Hohoema.Models.Helpers;

namespace Hohoema.Models.UseCase.Playlist
{



    // TODO: アイテム個数上限による失敗


    public class LoginUserOwnedMylistManager : ObservableObject
    {
        public LoginUserOwnedMylistManager(
            ILoggerFactory loggerFactory,
            NiconicoSession niconicoSession,
            LoginUserMylistProvider loginUserMylistProvider,
            NotificationService notificationService,
            LoginUserMylistItemIdRepository loginUserMylistItemIdRepository
            )
        {
            _logger = loggerFactory.CreateLogger<LoginUserOwnedMylistManager>();
            _niconicoSession = niconicoSession;
            _loginUserMylistProvider = loginUserMylistProvider;
            _notificationService = notificationService;
            _loginUserMylistItemIdRepository = loginUserMylistItemIdRepository;
            _mylists = new ObservableCollection<LoginUserMylistPlaylist>();
            Mylists = new ReadOnlyObservableCollection<LoginUserMylistPlaylist>(_mylists);

            _niconicoSession.LogIn += async (_, e) =>
            {
                _loginUserMylistItemIdRepository.Clear();

                try
                {
                    using (await _mylistSyncLock.LockAsync(default))
                    {
                        IsLoginUserMylistReady = false;

                        await SyncMylistGroups();

                        IsLoginUserMylistReady = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, "Login user mylist update failed.");
                }
            };

            _niconicoSession.LogOut += async (_, e) =>
            {
                try
                {
                    using (await _mylistSyncLock.LockAsync(default))
                    {
                        IsLoginUserMylistReady = false;

                        _mylists.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, "Logout user mylist update failed.");
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

        private readonly ILogger<LoginUserOwnedMylistManager> _logger;
        readonly private NiconicoSession _niconicoSession;
        readonly private LoginUserMylistProvider _loginUserMylistProvider;
        private readonly NotificationService _notificationService;
        private readonly LoginUserMylistItemIdRepository _loginUserMylistItemIdRepository;

        public LoginUserMylistPlaylist Deflist { get; private set; }

		private ObservableCollection<LoginUserMylistPlaylist> _mylists;
		public ReadOnlyObservableCollection<LoginUserMylistPlaylist> Mylists { get; private set; }

        private AsyncLock _updateLock = new AsyncLock();

        

        public int DeflistRegistrationCapacity => _niconicoSession.IsPremiumAccount ? 500 : 100;

        public int DeflistRegistrationCount => Deflist.Count;
        public int MylistRegistrationCapacity => _niconicoSession.IsPremiumAccount ? 25000 : 100;

        public int MylistRegistrationCount => Mylists.Where(x => !x.MylistId.IsWatchAfterMylist).Sum((System.Func<MylistPlaylist, int>)(x => (int)x.Count));

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
                _notificationService.ShowLiteInAppNotification_Fail("InAppNotification_MylistAddedItems_Fail".Translate(playlist.Name));
            }
            else
            {
                _notificationService.ShowLiteInAppNotification_Success("InAppNotification_MylistAddedItems_Success".Translate(playlist.Name, e.SuccessedItems.Count));
            }
        }

        private void Playlist_MylistItemRemoved(object sender, MylistItemRemovedEventArgs e)
        {
            var playlist = (LoginUserMylistPlaylist)sender;
            if (e.FailedItems?.Any() ?? false)
            {
                _notificationService.ShowLiteInAppNotification_Fail("InAppNotification_MylistRemovedItems_Fail".Translate(playlist.Name));
            }
            else
            {
                _notificationService.ShowLiteInAppNotification_Success("InAppNotification_MylistRemovedItems_Success".Translate(playlist.Name, e.SuccessedItems.Count));
            }
        }


        public bool HasMylistGroup(MylistId groupId)
		{
			return Mylists.Any(x => x.MylistId == groupId);
		}

        public async Task WaitUpdate(CancellationToken ct = default)
        {
            using var _ = await _updateLock.LockAsync(ct);
        }

		public LoginUserMylistPlaylist GetMylistGroup(MylistId groupId)
		{
			return Mylists.SingleOrDefault(x => x.MylistId == groupId);
		}

        public async Task<LoginUserMylistPlaylist> GetMylistGroupAsync(MylistId groupId, CancellationToken ct = default)
        {
            using (await _updateLock.LockAsync(ct))
            {
                return Mylists.SingleOrDefault(x => x.MylistId == groupId);
            }
        }


        public async Task SyncMylistGroups(CancellationToken ct = default)
		{
            using (var releaser = await _updateLock.LockAsync(ct))            
            {
                Deflist = null;

                foreach (var item in _mylists)
                {
                    RemoveHandleMylistItemChanged(item);
                }
                _mylists.Clear();

                if (_niconicoSession.IsLoggedIn)
                {
                    try
                    {
                        var groups = await _loginUserMylistProvider.GetLoginUserMylistGroups();

                        ct.ThrowIfCancellationRequested();

                        foreach (var mylistGroup in groups ?? Enumerable.Empty<LoginUserMylistPlaylist>())
                        {
                            if (mylistGroup.MylistId.IsWatchAfterMylist)
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

                foreach (var item in _mylists)
                {
                    HandleMylistItemChanged(item);
                }
            }
		}

		public async Task<LoginUserMylistPlaylist> AddMylist(string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            var result = await _loginUserMylistProvider.AddMylist(name, description, isPublic, sortKey, sortOrder);
            if (result != null)
            {
                await SyncMylistGroups();
                return _mylists.FirstOrDefault(x => x.MylistId == result);
            }
            else
            {
                return null;
            }
        }

		
		public async Task<bool> RemoveMylist(MylistId mylistId)
		{
			var result = await _loginUserMylistProvider.RemoveMylist(mylistId);

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
