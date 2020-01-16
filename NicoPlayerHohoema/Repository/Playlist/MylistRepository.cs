using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public MylistPlaylist(string id, string label, int count)
        {
            Id = id;
            Label = label;
            Count = count;
        }

        public string Id { get; }

        public string Label { get; }

        public int Count { get; }

        public int SortIndex { get; internal set; }

        public string Description { get; internal set; }

        public string UserId { get; internal set; }

        public bool IsPublic { get; internal set; }

        public IconType IconType { get; internal set; }

        public Order Order { get; internal set; }

        public Sort Sort { get; internal set; }

        public DateTime UpdateTime { get; internal set; }

        public DateTime CreateTime { get; internal set; }
    }

    public class LoginUserMylistPlaylist : MylistPlaylist
    {
        public LoginUserMylistPlaylist(string id, string label, int count)
            : base(id, label, count)
        {
            Label = label;
            Count = count;
        }

        public new string Label { get; internal set; }

        public new int Count { get; internal set; }

        public MylistDefaultSort DefaultSort { get; internal set; }
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


        public async Task<MylistItemsGetResult> GetItemsAsync(IMylist mylist, int start, int count)
        {
            // ログインユーザーのマイリストかどうかをチェック
            if (_niconicoSession.UserIdString == mylist.UserId)
            {
                var items = await _userMylistManager.GetLoginUserMylistItemsAsync(mylist);
                return new MylistItemsGetResult()
                {
                    IsSuccess = true,
                    TotalCount = items.Count,
                    IsDefaultMylist = mylist.IsDefaultMylist(),
                    IsLoginUserMylist = true,
                    Items = items,
                    ItemsHeadPosition = 0,
                    Mylist = mylist,
                };
            }
            else
            {
                if (mylist.IsDefaultMylist())
                {
                    throw new ArgumentException("とりあえずマイリストはログインしていなければアクセスできません。");
                }

                if (!mylist.IsPublic)
                {
                    throw new ArgumentException("非公開マイリストはアクセスできません。");
                }

                // 他ユーザーマイリストとして取得を実行
                try
                {
                    var result = await _otherOwneredMylistManager.GetMylistItemsWithRangeAsync(mylist, start, count);

                    return new MylistItemsGetResult()
                    {
                        IsSuccess = true,
                        IsDefaultMylist = mylist.IsDefaultMylist(),
                        Mylist = mylist,
                        IsLoginUserMylist = false,
                        Items = result.Items,
                        ItemsHeadPosition = result.HeadPosition,
                        TotalCount = result.TotalCount,
                    };
                }
                catch
                {

                }
            }

            return new MylistItemsGetResult() { IsSuccess = false };
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
                var mylist = new MylistPlaylist(detail.Id, detail.Name, (int)detail.Count)
                {
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
                return new MylistPlaylist(x.Id, x.Name, x.Count)
                {
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


        public Task<MylistProvider.MylistItemsGetResult> GetMylistItemsWithRangeAsync(IMylist mylist, int start, int count)
        {
            return MylistProvider.GetMylistGroupVideo(mylist.Id, start, count);
        }
    }
}
