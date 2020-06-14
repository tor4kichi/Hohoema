using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.UseCase.Playlist.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;

namespace NicoPlayerHohoema.Repository.Playlist
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

    public class MylistPlaylist : IMylist
    {
        private readonly MylistProvider _mylistProvider;

        public MylistPlaylist(string id)
        {
            Id = id;
        }

        public MylistPlaylist(string id, MylistProvider mylistProvider)
        {
            Id = id;
            _mylistProvider = mylistProvider;
        }

        public string Id { get; }

        public string Label { get; internal set; }

        public int Count { get; internal set; }

        public int SortIndex { get; internal set; }

        public string Description { get; internal set; }

        public string UserId { get; internal set; }

        public bool IsPublic { get; internal set; }

        public IconType IconType { get; internal set; }

        public Order Order { get; internal set; }

        public Sort Sort { get; internal set; }

        public DateTime UpdateTime { get; internal set; }

        public DateTime CreateTime { get; internal set; }


        public async Task<MylistItemsGetResult> GetItemsAsync(int start, int count)
        {
           
            //if (this.IsDefaultMylist())
            {
                //throw new ArgumentException("とりあえずマイリストはログインしていなければアクセスできません。");
            }

            if (!IsPublic)
            {
                throw new ArgumentException("非公開マイリストはアクセスできません。");
            }

            // 他ユーザーマイリストとして取得を実行
            try
            {
                var result = await GetMylistItemsWithRangeAsync(start, count);

                return new MylistItemsGetResult()
                {
                    IsSuccess = true,
                    IsDefaultMylist = this.IsDefaultMylist(),
                    Mylist = this,
                    IsLoginUserMylist = false,
                    Items = result.Items,
                    ItemsHeadPosition = result.HeadPosition,
                    TotalCount = result.TotalCount,
                };
            }
            catch
            {

            }

            return new MylistItemsGetResult() { IsSuccess = false };

        }

        public Task<MylistProvider.MylistItemsGetResult> GetMylistItemsWithRangeAsync(int start, int count)
        {
            return _mylistProvider.GetMylistGroupVideo(Id, start, count);
        }

        public async Task<MylistProvider.MylistItemsGetResult> GetMylistAllItems()
        {
            var firstResult = await _mylistProvider.GetMylistGroupVideo(Id, 0, 150);
            if (!firstResult.IsSuccess || firstResult.TotalCount == firstResult.Items.Count)
            {
                return firstResult;
            }

            var itemsList = new List<IVideoContent>(firstResult.Items);
            var totalCount = firstResult.TotalCount;
            var currentCount = firstResult.Items.Count;
            do
            {
                await Task.Delay(500);
                var result = await _mylistProvider.GetMylistGroupVideo(Id, currentCount, 150);
                if (result.IsSuccess)
                {
                    itemsList.AddRange(result.Items);
                }

                currentCount += result.Items.Count;
            }
            while (currentCount < totalCount);

            return new MylistProvider.MylistItemsGetResult()
            {
                MylistId = Id,
                HeadPosition = 0,
                TotalCount = totalCount,
                IsSuccess = true,
                Items = new ReadOnlyCollection<IVideoContent>(itemsList)
            };
        }
    }

    public class LoginUserMylistPlaylist : MylistPlaylist
    {
        LoginUserMylistProvider _loginUserMylistProvider;

        public LoginUserMylistPlaylist(string id, LoginUserMylistProvider loginUserMylistProvider)
            : base(id)
        {
            _loginUserMylistProvider = loginUserMylistProvider;
            ItemsRemoveCommand = new MylistRemoveItemCommand(this);
            ItemsAddCommand = new MylistAddItemCommand(this, App.Current.Container.Resolve<NotificationService>());
        }

        public MylistRemoveItemCommand ItemsRemoveCommand { get; }
        public MylistAddItemCommand ItemsAddCommand { get; }

        public MylistDefaultSort DefaultSort { get; internal set; }


        public Task<List<IVideoContent>> GetLoginUserMylistItemsAsync()
        {
            return _loginUserMylistProvider.GetLoginUserMylistItemsAsync(this);
        }


        public Task<ContentManageResult> UpdateMylist(Dialogs.MylistGroupEditData editData)
        {
            return _loginUserMylistProvider.UpdateMylist(Id, editData);
        }




        public Task<MylistItemAddedEventArgs> AddItem(string videoId, string mylistComment = "")
        {
            return AddItem(new[] { videoId }, mylistComment);
        }

        public async Task<MylistItemAddedEventArgs> AddItem(IEnumerable<string> items, string mylistComment = "")
        {
            List<string> successed = new List<string>();
            List<string> failed = new List<string>();

            foreach (var videoId in items)
            {
                var result = await _loginUserMylistProvider.AddMylistItem(Id, videoId, mylistComment);
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
                MylistId = Id,
                SuccessedItems = successed,
                FailedItems = failed
            };
            MylistItemAdded?.Invoke(this, args);

            return args;
        }


        public Task<MylistItemRemovedEventArgs> RemoveItem(string videoId)
        {
            return RemoveItem(new[] { videoId });
        }

        public async Task<MylistItemRemovedEventArgs> RemoveItem(IEnumerable<string> items)
        {
            List<string> successed = new List<string>();
            List<string> failed = new List<string>();

            foreach (var videoId in items)
            {
                var result = await _loginUserMylistProvider.RemoveMylistItem(Id, videoId);
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
                MylistId = Id,
                SuccessedItems = successed,
                FailedItems = failed
            };
            MylistItemRemoved?.Invoke(this, args);

            return args;
        }

        public event EventHandler<MylistItemAddedEventArgs> MylistItemAdded;
        public event EventHandler<MylistItemRemovedEventArgs> MylistItemRemoved;


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
        private readonly Models.NiconicoSession _niconicoSession;
        private readonly UserMylistManager _userMylistManager;
        private readonly OtherOwneredMylistManager _otherOwneredMylistManager;

        public MylistRepository(
            Models.NiconicoSession niconicoSession,
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
            if (_userMylistManager.HasMylistGroup(mylistId))
            {
                return _userMylistManager.GetMylistGroup(mylistId);
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

    public class OtherOwneredMylistManager
    {
        public OtherOwneredMylistManager(
            MylistProvider mylistProvider,
            UserProvider userProvider
            )
        {
            MylistProvider = mylistProvider;
            UserProvider = userProvider;
        }

        public MylistProvider MylistProvider { get; }
        public UserProvider UserProvider { get; }

        public async Task<MylistPlaylist> GetMylist(string mylistGroupId)
        {
            var res = await MylistProvider.GetMylistGroupDetail(mylistGroupId);
            if (res.IsOK)
            {
                var detail = res.MylistGroup;
                var mylist = new MylistPlaylist(detail.Id, MylistProvider)
                {
                    Label = detail.Name,
                    Count = (int)detail.Count,
                    IconType = detail.GetIconType(),
                    CreateTime = detail.CreateTime,
                    UpdateTime = detail.UpdateTime,
                    Order = detail.GetSortOrder(),
                    IsPublic = detail.IsPublic,
                    SortIndex = 0,
                    UserId = detail.UserId,
                    Description = detail.Description,
                };
                return mylist;
            }
            else
            {
                return null;
            }
        }

        public async Task<List<MylistPlaylist>> GetByUserId(string userId)
        {
            var groups = await UserProvider.GetUserMylistGroups(userId);
            if (groups == null) { return null; }

            var list = groups.Select((x, i) =>
            {
                return new MylistPlaylist(x.Id, MylistProvider)
                {
                    Label = x.Name,
                    Count = x.Count,
                    SortIndex = i,
                    UserId = x.UserId,
                    Description = x.Description,
                    IsPublic = x.GetIsPublic(),
                    IconType = x.GetIconType(),
                };
            }
            ).ToList();

            return list;
        }
    }
}
