using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Provider
{
    public sealed class LoginUserMylistProvider : ProviderBase
    {
        public LoginUserMylistProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }


        private async Task<UserOwnedMylist> GetDefaultMylistAsync()
        {
            if (!NiconicoSession.IsLoggedIn) { throw new System.Exception("");  }

            var defMylist = await Context.User.GetMylistItemListAsync(UserOwnedMylist.DefailtMylistId);

            List<Database.NicoVideo> videoItems = new List<Database.NicoVideo>();
            foreach (var item in defMylist)
            {
                if (item.ItemType == NiconicoItemType.Video)
                {
                    var video = MylistDataToNicoVideoData(item);

                    videoItems.Add(video);
                }
            }

            var mylist = new UserOwnedMylist(UserOwnedMylist.DefailtMylistId, videoItems.Select(x => x.RawVideoId), this)
            {
                Label = "とりあえずマイリスト",
                UserId = NiconicoSession.UserId.ToString(),
                IsPublic = false,
                Sort = MylistDefaultSort.Latest,
            };

            _ = Task.Run(() =>
            {
                var mylistId = UserOwnedMylist.DefailtMylistId;
                Database.Temporary.MylistDb.AddItemId(
                    defMylist.Select(x => new Database.Temporary.MylistItemIdContainer()
                    {
                        MylistGroupId = mylistId,
                        VideoId = x.WatchId,
                        ItemId = x.ItemId
                    }));
            });

            return mylist;
        }

        public async Task<List<UserOwnedMylist>> GetLoginUserMylistGroups()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return null;
            }

            await WaitNicoPageAccess();

            List<UserOwnedMylist> mylistGroups = new List<UserOwnedMylist>();

            {
                var defaultMylist = await GetDefaultMylistAsync();

                mylistGroups.Add(defaultMylist);
            }

            var res = await Context.User.GetMylistGroupListAsync();

            foreach (var mylistGroup in res ?? Enumerable.Empty<MylistGroupData>())
            {
                await WaitNicoPageAccess();

                var mylistItems = await Context.User.GetMylistItemListAsync(mylistGroup.Id);

                var videos = mylistItems.Select(x => MylistDataToNicoVideoData(x).RawVideoId);
                var mylist = new UserOwnedMylist(mylistGroup.Id, videos, this)
                {
                    UserId = mylistGroup.UserId,
                    Label = mylistGroup.Name,
                    Description = mylistGroup.Description,
                    IsPublic = mylistGroup.GetIsPublic(),
                    IconType = mylistGroup.GetIconType(),
                    ThumnailUrls = mylistGroup.ThumbnailUrls.ToList(),
                };

                _ = Task.Run(() => 
                {
                    var mylistId = mylistGroup.Id;
                    Database.Temporary.MylistDb.AddItemId(
                        mylistItems.Select(x => new Database.Temporary.MylistItemIdContainer()
                        {
                            MylistGroupId = mylistId,
                            VideoId = x.WatchId,
                            ItemId = x.ItemId
                        }));
                });

                mylistGroups.Add(mylist);
            }

            return mylistGroups;
        }


        static private Database.NicoVideo MylistDataToNicoVideoData(MylistData item)
        {
            var video = Database.NicoVideoDb.Get(item.WatchId)
                        ?? new Database.NicoVideo() { RawVideoId = item.WatchId };

            video.VideoId = item.ItemId;
            video.Title = item.Title;


            // TODO

            Database.NicoVideoDb.AddOrUpdate(video);

            return video;
        }


        public async Task<ContentManageResult> AddMylist(string name, string description, bool is_public, MylistDefaultSort default_sort, IconType iconType)
        {
            return await Context.User.CreateMylistGroupAsync(name, description, is_public, default_sort, iconType);
        }


        public async Task<ContentManageResult> RemoveMylist(string group_id)
        {
            return await Context.User.RemoveMylistGroupAsync(group_id);
        }




        public async Task<ContentManageResult> AddMylistItem(string mylistGroupId, string videoId, string mylistComment = "")
        {
            return await Context.User.AddMylistItemAsync(
                mylistGroupId
                , Mntone.Nico2.NiconicoItemType.Video
                , videoId
                , mylistComment
                );
        }


        public async Task<ContentManageResult> RemoveMylistItem(string mylistGroupid, string videoId)
        {
            var itemIdContainer = Database.Temporary.MylistDb.GetItemId(mylistGroupid, videoId);
            if (itemIdContainer == null) { return ContentManageResult.Failed; }
            return await Context.User.RemoveMylistItemAsync(mylistGroupid, NiconicoItemType.Video, itemIdContainer.ItemId);
        }

        public async Task<ContentManageResult> CopyMylistTo(string sourceMylistGroupId, UserOwnedMylist targetGroupInfo, params string[] videoIdList)
        {
            var items = Database.Temporary.MylistDb.GetItemIdList(sourceMylistGroupId, videoIdList);
            return await Context.User.CopyMylistItemAsync(sourceMylistGroupId, targetGroupInfo.GroupId, NiconicoItemType.Video, items.Select(x => x.ItemId).ToArray());
        }


        public async Task<ContentManageResult> MoveMylistTo(string sourceMylistGroupId, UserOwnedMylist targetGroupInfo, params string[] videoIdList)
        {
            var items = Database.Temporary.MylistDb.GetItemIdList(sourceMylistGroupId, videoIdList);
            return await Context.User.MoveMylistItemAsync(sourceMylistGroupId, targetGroupInfo.GroupId, NiconicoItemType.Video, items.Select(x => x.ItemId).ToArray());
        }


        public async Task<ContentManageResult> UpdateMylist(UserOwnedMylist mylist)
        {
            if (mylist.IsDeflist)
            {
                throw new Exception();
            }

            return await Context.User.UpdateMylistGroupAsync(
                mylist.GroupId, 
                mylist.Label,
                mylist.Description,
                mylist.IsPublic,
                mylist.Sort, 
                mylist.IconType
                );
        }
    }

}
