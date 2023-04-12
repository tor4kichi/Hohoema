#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Contracts.Services;
using Hohoema.Helpers;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Mylist.LoginUser;
using Microsoft.Extensions.Logging;
using NiconicoToolkit.Mylist;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Hohoema.Models.Niconico.Mylist.LoginUser.LoginUserMylistProvider;

namespace Hohoema.Services.Niconico;

// TODO: アイテム個数上限による失敗

public class LoginUserOwnedMylistManager : ObservableObject
{
    public LoginUserOwnedMylistManager(
        ILoggerFactory loggerFactory,
        ILocalizeService localizeService,
        NiconicoSession niconicoSession,
        LoginUserMylistProvider loginUserMylistProvider,
        INotificationService notificationService,
        LoginUserMylistItemIdRepository loginUserMylistItemIdRepository
        )
    {
        _logger = loggerFactory.CreateLogger<LoginUserOwnedMylistManager>();
        _localizeService = localizeService;
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
                _logger.LogError(ex, "Login user mylist update failed.");
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
                _logger.LogError(ex, "Logout user mylist update failed.");
            }
        };
    }

    private readonly AsyncLock _mylistSyncLock = new();

    private bool _IsLoginUserMylistReady;
    public bool IsLoginUserMylistReady
    {
        get => _IsLoginUserMylistReady;
        set => SetProperty(ref _IsLoginUserMylistReady, value);
    }

    private readonly ILogger<LoginUserOwnedMylistManager> _logger;
    private readonly ILocalizeService _localizeService;
    private readonly NiconicoSession _niconicoSession;
    private readonly LoginUserMylistProvider _loginUserMylistProvider;
    private readonly INotificationService _notificationService;
    private readonly LoginUserMylistItemIdRepository _loginUserMylistItemIdRepository;

    public LoginUserMylistPlaylist Deflist { get; private set; }

    private readonly ObservableCollection<LoginUserMylistPlaylist> _mylists;
    public ReadOnlyObservableCollection<LoginUserMylistPlaylist> Mylists { get; private set; }

    private readonly AsyncLock _updateLock = new();



    public int DeflistRegistrationCapacity => _niconicoSession.IsPremiumAccount ? 500 : 100;

    public int DeflistRegistrationCount => Deflist.Count;
    public int MylistRegistrationCapacity => _niconicoSession.IsPremiumAccount ? 25000 : 100;

    public int MylistRegistrationCount => Mylists.Where(x => !x.MylistId.IsWatchAfterMylist).Sum((System.Func<MylistPlaylist, int>)(x => x.Count));

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
        LoginUserMylistPlaylist playlist = (LoginUserMylistPlaylist)sender;
        if (e.FailedItems?.Any() ?? false)
        {
            _notificationService.ShowLiteInAppNotification_Fail(_localizeService.Translate("InAppNotification_MylistAddedItems_Fail", playlist.Name));
        }
        else
        {
            _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("InAppNotification_MylistAddedItems_Success", playlist.Name, e.SuccessedItems.Count));
        }
    }

    private void Playlist_MylistItemRemoved(object sender, MylistItemRemovedEventArgs e)
    {
        LoginUserMylistPlaylist playlist = (LoginUserMylistPlaylist)sender;
        if (e.FailedItems?.Any() ?? false)
        {
            _notificationService.ShowLiteInAppNotification_Fail(_localizeService.Translate("InAppNotification_MylistRemovedItems_Fail", playlist.Name));
        }
        else
        {
            _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("InAppNotification_MylistRemovedItems_Success", playlist.Name, e.SuccessedItems.Count));
        }
    }


    public bool HasMylistGroup(MylistId groupId)
    {
        return Mylists.Any(x => x.MylistId == groupId);
    }

    public async Task WaitUpdate(CancellationToken ct = default)
    {
        using IDisposable _ = await _updateLock.LockAsync(ct);
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
        using IDisposable releaser = await _updateLock.LockAsync(ct);
        Deflist = null;

        foreach (LoginUserMylistPlaylist item in _mylists)
        {
            RemoveHandleMylistItemChanged(item);
        }
        _mylists.Clear();

        if (_niconicoSession.IsLoggedIn)
        {
            try
            {
                System.Collections.Generic.List<LoginUserMylistPlaylist> groups = await _loginUserMylistProvider.GetLoginUserMylistGroups();

                ct.ThrowIfCancellationRequested();

                foreach (LoginUserMylistPlaylist mylistGroup in groups ?? Enumerable.Empty<LoginUserMylistPlaylist>())
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

        foreach (LoginUserMylistPlaylist item in _mylists)
        {
            HandleMylistItemChanged(item);
        }
    }

    public async Task<LoginUserMylistPlaylist> AddMylist(string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
    {
        string result = await _loginUserMylistProvider.AddMylist(name, description, isPublic, sortKey, sortOrder);
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
        bool result = await _loginUserMylistProvider.RemoveMylist(mylistId);

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
