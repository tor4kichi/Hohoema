﻿using Mntone.Nico2;
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
using Mntone.Nico2.Users.Mylist;
using I18NPortable;

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

        public MylistSortKey DefaultSortKey { get; internal set; }
        public MylistSortOrder DefaultSortOrder { get; internal set; }


        
        public DateTime CreateTime { get; internal set; }


        public async Task<MylistItemsGetResult> GetItemsAsync(MylistSortKey sortKey, MylistSortOrder sortOrder, uint pageSize, uint page)
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
                var result = await GetMylistItemsWithRangeAsync(sortKey, sortOrder, pageSize, page);

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

        public Task<MylistProvider.MylistItemsGetResult> GetMylistItemsWithRangeAsync(MylistSortKey sortKey, MylistSortOrder sortOrder, uint pageSize, uint page)
        {
            return _mylistProvider.GetMylistGroupVideo(Id, sortKey, sortOrder, pageSize, page);
        }

        public async Task<MylistProvider.MylistItemsGetResult> GetMylistAllItems(MylistSortKey sortKey = MylistSortKey.AddedAt, MylistSortOrder sortOrder = MylistSortOrder.Asc)
        {
            uint page = 0;
            const int pageSize = 25;

            var firstResult = await _mylistProvider.GetMylistGroupVideo(Id, sortKey, sortOrder, pageSize, page);
            if (!firstResult.IsSuccess || firstResult.TotalCount == firstResult.Items.Count)
            {
                return firstResult;
            }

            page++;

            var itemsList = new List<Database.NicoVideo>(firstResult.Items);
            var totalCount = firstResult.TotalCount;
            var currentCount = firstResult.Items.Count;
            do
            {
                await Task.Delay(500);
                var result = await _mylistProvider.GetMylistGroupVideo(Id, sortKey, sortOrder, pageSize, page);
                if (result.IsSuccess)
                {
                    itemsList.AddRange(result.Items);
                }

                page++;
                currentCount += result.Items.Count;
            }
            while (currentCount < totalCount);

            return new MylistProvider.MylistItemsGetResult()
            {
                MylistId = Id,
                HeadPosition = 0,
                TotalCount = totalCount,
                IsSuccess = true,
                Items = new ReadOnlyCollection<Database.NicoVideo>(itemsList)
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

        public async Task<List<IVideoContent>> GetAll(MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            List<IVideoContent> items = new List<IVideoContent>();
            uint page = 0;

            while (items.Count != Count)
            {
                var res = await _loginUserMylistProvider.GetLoginUserMylistItemsAsync(this, sortKey, sortOrder, 25, page);
                items.AddRange(res);
                page++;
            }

            return items;
        }

        public Task<List<IVideoContent>> GetLoginUserMylistItemsAsync(MylistSortKey sortKey, MylistSortOrder sortOrder, uint pageSize, uint page)
        {
            return _loginUserMylistProvider.GetLoginUserMylistItemsAsync(this, sortKey, sortOrder, pageSize, page);
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
            var detail = await MylistProvider.GetMylistGroupDetail(mylistGroupId);
            
            if (mylistGroupId == "0")
            {
                var mylist = new MylistPlaylist(detail.Id.ToString(), MylistProvider)
                {
                    Label = detail.Name ?? "DefaultMylist".Translate(),
                    Count = (int)detail.TotalItemCount,
                    IsPublic = true
                };
                return mylist;
            }
            else
            {
                var mylist = new MylistPlaylist(detail.Id.ToString(), MylistProvider)
                {
                    Label = detail.Name,
                    Count = (int)detail.TotalItemCount,
                    CreateTime = detail.CreatedAt.DateTime,
                    //DefaultSortOrder = ,
                    IsPublic = detail.IsPublic,
                    SortIndex = 0,
                    UserId = detail.Owner.Id,
                    Description = detail.Description,
                };
                return mylist;
            }
        }

        public async Task<List<MylistPlaylist>> GetByUserId(string userId)
        {
            var groups = await UserProvider.GetUserMylistGroups(userId);
            if (groups == null) { return null; }

            var list = groups.Data.Mylists.Select((x, i) =>
            {
                return new MylistPlaylist(x.Id.ToString(), MylistProvider)
                {
                    Label = x.Name,
                    Count = (int)x.TotalItemCount,
                    SortIndex = i,
                    UserId = x.Owner.Id,
                    Description = x.Description,
                    IsPublic = x.IsPublic,
                    CreateTime = x.CreatedAt.DateTime,
                    DefaultSortKey = x.DefaultSortKey,
                    DefaultSortOrder = x.DefaultSortOrder,
                };
            }
            ).ToList();

            return list;
        }
    }
}
