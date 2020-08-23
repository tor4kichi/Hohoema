using I18NPortable;
using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Users.Mylist;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Repository.Playlist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Provider
{
    public sealed class LoginUserMylistProvider : ProviderBase
    {

        static LoginUserMylistProvider()
        {
            Database.Temporary.MylistDb.Clear();
        }

        public LoginUserMylistProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
            
        }


        private async Task<LoginUserMylistPlaylist> GetDefaultMylistAsync()
        {
            if (!NiconicoSession.IsLoggedIn) { throw new System.Exception("");  }
            
            var defMylist = await ContextActionAsync(async context =>
            {
                return await context.User.GetWatchAfterMylistGroupItemsAsync(MylistSortKey.AddedAt, MylistSortOrder.Asc, 3, 0);
            });

            // TODO: とりあえずマイリストのSortやOrderの取得

            return new LoginUserMylistPlaylist(MylistPlaylistExtension.DefailtMylistId, this) 
            {
                Label = "DefaultMylist".Translate(),
                Count = (int)defMylist.Data.Mylist.TotalItemCount,
                UserId = NiconicoSession.UserIdString 
            };
        }

        public async Task<List<LoginUserMylistPlaylist>> GetLoginUserMylistGroups()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return null;
            }

            List<LoginUserMylistPlaylist> mylistGroups = new List<LoginUserMylistPlaylist>();

            var defaultMylist = await GetDefaultMylistAsync();

            mylistGroups.Add(defaultMylist);

            var res = await ContextActionAsync(async context =>
            {
                return await context.User.GetLoginUserMylistGroupsAsync();
            });

            if (res.Meta.Status != 200)
            {
                return mylistGroups;
            }
            
            foreach (var mylistGroup in res.Data.Mylists)
            {
                var mylist = new LoginUserMylistPlaylist(mylistGroup.Id.ToString(), this)
                {
                    Label = mylistGroup.Name,
                    Count = (int)mylistGroup.TotalItemCount,
                    UserId = mylistGroup.Owner.Id,
                    Description = mylistGroup.Description,
                    IsPublic = mylistGroup.IsPublic,
                    //IconType = mylistGroup.co,
                    DefaultSortKey = mylistGroup.DefaultSortKey,
                    DefaultSortOrder = mylistGroup.DefaultSortOrder,
                    SortIndex = res.Data.Mylists.IndexOf(mylistGroup)
                };

                mylistGroups.Add(mylist);
            }

            return mylistGroups;
        }

        public async Task<List<IVideoContent>> GetLoginUserMylistItemsAsync(IMylist mylist, MylistSortKey sortKey, MylistSortOrder sortOrder, uint pageSize, uint page)
        {
            if (mylist.UserId != NiconicoSession.UserIdString)
            {
                throw new ArgumentException();
            }

            if (mylist.IsDefaultMylist())
            {
                var res = await ContextActionAsync(async context =>
                {
                    var mylistItemsRes = await context.User.GetWatchAfterMylistGroupItemsAsync(sortKey, sortOrder, pageSize, page);
                    return mylistItemsRes.Data.Mylist;
                });

                var items = res.Items;
                Database.Temporary.MylistDb.AddItemId(items.Select(x => new Database.Temporary.MylistItemIdContainer()
                {
                    MylistGroupId = mylist.Id,
                    VideoId = x.WatchId,
                    ItemId = x.ItemId.ToString(),
                }));

                return items.Select(x => MylistDataToNicoVideoData(x)).Cast<IVideoContent>().ToList();

            }
            else
            {
                var res = await ContextActionAsync(async context =>
                {
                    var mylistItemsRes = await context.User.GetLoginUserMylistGroupItemsAsync(int.Parse(mylist.Id), sortKey, sortOrder, pageSize, page);
                    return mylistItemsRes.Data.Mylist;
                });

                var items = res.Items;
                Database.Temporary.MylistDb.AddItemId(items.Select(x => new Database.Temporary.MylistItemIdContainer()
                {
                    MylistGroupId = mylist.Id,
                    VideoId = x.WatchId,
                    ItemId = x.ItemId.ToString(),
                }));

                return items.Select(x => MylistDataToNicoVideoData(x)).Cast<IVideoContent>().ToList();
            }
        }


        static public bool IsDefaultMylist(IMylist mylist)
        {
            return mylist?.Id == UserOwnedMylist.DefailtMylistId;
        }


        static private Database.NicoVideo MylistDataToNicoVideoData(Item item)
        {
            var video = Database.NicoVideoDb.Get(item.WatchId)
                        ?? new Database.NicoVideo() { RawVideoId = item.WatchId };

            video.RawVideoId = item.WatchId;
            video.VideoId = item.WatchId;
            video.Title = item.Video.Title;
            video.Description = item.Description;
            video.IsDeleted = item.IsDeleted;
            video.Length = TimeSpan.FromSeconds(item.Video.Duration);
            video.PostedAt = item.Video.RegisteredAt.DateTime;

            video.ThumbnailUrl = item.Video.Thumbnail.ListingUrl.OriginalString;
            video.ViewCount = (int)item.Video.Count.View;
            video.MylistCount = (int)item.Video.Count.Mylist;
            video.CommentCount = (int)item.Video.Count.Comment;

            Database.NicoVideoDb.AddOrUpdate(video);

            return video;
        }


        public async Task<ContentManageResult> AddMylist(string name, string description, bool is_public, MylistDefaultSort default_sort, IconType iconType)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.User.CreateMylistGroupAsync(name, description, is_public, default_sort, iconType);
            });
        }


        public async Task<ContentManageResult> RemoveMylist(string group_id)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.User.RemoveMylistGroupAsync(group_id);
            });
        }




        public async Task<ContentManageResult> AddMylistItem(string mylistGroupId, string videoId, string mylistComment = "")
        {
            return await ContextActionAsync(async context =>
            {
                return await context.User.AddMylistItemAsync(
                    mylistGroupId
                    , Mntone.Nico2.NiconicoItemType.Video
                    , videoId
                    , mylistComment
                    );
            });
        }


        public async Task<ContentManageResult> RemoveMylistItem(string mylistGroupid, string videoId)
        {
            var itemIdContainer = Database.Temporary.MylistDb.GetItemId(mylistGroupid, videoId);
            if (itemIdContainer == null) { return ContentManageResult.Failed; }
            return await ContextActionAsync(async context =>
            {
                return await context.User.RemoveMylistItemAsync(mylistGroupid, NiconicoItemType.Video, itemIdContainer.ItemId);
            });
        }

        public async Task<ContentManageResult> CopyMylistTo(string sourceMylistGroupId, UserOwnedMylist targetGroupInfo, params string[] videoIdList)
        {
            var items = Database.Temporary.MylistDb.GetItemIdList(sourceMylistGroupId, videoIdList);
            return await ContextActionAsync(async context =>
            {
                return await context.User.CopyMylistItemAsync(sourceMylistGroupId, targetGroupInfo.GroupId, NiconicoItemType.Video, items.Select(x => x.ItemId).ToArray());
            });
        }


        public async Task<ContentManageResult> MoveMylistTo(string sourceMylistGroupId, UserOwnedMylist targetGroupInfo, params string[] videoIdList)
        {
            var items = Database.Temporary.MylistDb.GetItemIdList(sourceMylistGroupId, videoIdList);
            return await ContextActionAsync(async context =>
            {
                return await context.User.MoveMylistItemAsync(sourceMylistGroupId, targetGroupInfo.GroupId, NiconicoItemType.Video, items.Select(x => x.ItemId).ToArray());
            });
        }


        public async Task<ContentManageResult> UpdateMylist(string myylistId, Dialogs.MylistGroupEditData editData)
        {
            if (myylistId == "0")
            {
                throw new Exception();
            }

            throw new NotImplementedException();
            /*
            return await ContextActionAsync(async context =>
            {
                return await context.User.UpdateMylistGroupAsync(
                    myylistId,
                    editData.Name,
                    editData.Description,
                    editData.IsPublic,
                    editData.DefaultSortKey,
                    IconType.Default
                    );
                
            });
            */
        }
    }

}
