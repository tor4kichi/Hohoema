using I18NPortable;
using Mntone.Nico2;
using Mntone.Nico2.Mylist;
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
                return await context.User.GetMylistItemListAsync(MylistPlaylistExtension.DefailtMylistId);
            });

            // TODO: とりあえずマイリストのSortやOrderの取得

            return new LoginUserMylistPlaylist(MylistPlaylistExtension.DefailtMylistId, this) 
            {
                Label = "DefaultMylist".Translate(),
                Count = defMylist.Count,
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
                return await context.User.GetMylistGroupListAsync();
            });            

            foreach (var mylistGroup in res ?? Enumerable.Empty<LoginUserMylistGroup>())
            {
                var mylist = new LoginUserMylistPlaylist(mylistGroup.Id, this)
                {
                    Label = mylistGroup.Name,
                    Count = mylistGroup.Count,
                    UserId = mylistGroup.UserId,
                    Description = mylistGroup.Description,
                    IsPublic = mylistGroup.GetIsPublic(),
                    IconType = mylistGroup.GetIconType(),
                    DefaultSort = mylistGroup.GetDefaultSort(),
                    SortIndex = res.IndexOf(mylistGroup)
                };

                mylistGroups.Add(mylist);
            }

            return mylistGroups;
        }

        public async Task<List<IVideoContent>> GetLoginUserMylistItemsAsync(IMylist mylist)
        {
            if (mylist.UserId != NiconicoSession.UserIdString)
            {
                throw new ArgumentException();
            }

            var items = await ContextActionAsync(async context =>
            {
                return await context.User.GetMylistItemListAsync(mylist.Id);
            });

            Database.Temporary.MylistDb.AddItemId(items.Select(x => new Database.Temporary.MylistItemIdContainer()
            {
                MylistGroupId = mylist.Id,
                VideoId = x.WatchId,
                ItemId = x.ItemId
            }));

            return items.Select(x => MylistDataToNicoVideoData(x)).Cast<IVideoContent>().ToList();
        }


        static public bool IsDefaultMylist(IMylist mylist)
        {
            return mylist?.Id == UserOwnedMylist.DefailtMylistId;
        }


        static private Database.NicoVideo MylistDataToNicoVideoData(MylistData item)
        {
            var video = Database.NicoVideoDb.Get(item.WatchId)
                        ?? new Database.NicoVideo() { RawVideoId = item.WatchId };

            video.RawVideoId = item.WatchId;
            video.VideoId = item.WatchId;
            video.Title = item.Title;
            video.Description = item.Description;
            video.IsDeleted = item.IsDeleted;
            video.Length = item.Length;

            video.ThumbnailUrl = item.ThumbnailUrl.OriginalString;
            video.ViewCount = (int)item.ViewCount;
            video.MylistCount = (int)item.MylistCount;
            video.CommentCount = (int)item.CommentCount;

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

            return await ContextActionAsync(async context =>
            {
                return await context.User.UpdateMylistGroupAsync(
                    myylistId,
                    editData.Name,
                    editData.Description,
                    editData.IsPublic,
                    editData.MylistDefaultSort,
                    editData.IconType
                    );
            });
        }
    }

}
