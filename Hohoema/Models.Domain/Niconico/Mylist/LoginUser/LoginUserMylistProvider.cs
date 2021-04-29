using I18NPortable;
using LiteDB;
using Mntone.Nico2;
using Mntone.Nico2.Users.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Mylist.LoginUser
{
    public sealed class LoginUserMylistProvider : ProviderBase
    {
        public sealed class LoginUserMylistItemIdRepository : LiteDBServiceBase<LoginUserMylistItemIdEntry>
        {
            public LoginUserMylistItemIdRepository(LiteDatabase liteDatabase) : base(liteDatabase)
            {
                _collection.EnsureIndex(x => x.VideoId);
                _collection.EnsureIndex(x => x.MylistGroupId);
            }

            public void AddItem(string itemId, string mylistId, string videoId)
            {
                _collection.Upsert(new LoginUserMylistItemIdEntry() { ItemId = itemId, MylistGroupId = mylistId, VideoId = videoId });
            }

            public string GetItemId(string mylistId, string videoId)
            {
                return _collection.FindOne(x => x.MylistGroupId == mylistId && x.VideoId == videoId)?.ItemId;
            }
        }

        public sealed class LoginUserMylistItemIdEntry
        {
            [BsonId]
            public string ItemId { get; set; }

            [BsonField]
            public string MylistGroupId { get; set; }

            [BsonField]
            public string VideoId { get; set; }
        }



        private readonly NicoVideoCacheRepository _nicoVideoRepository;
        private readonly LoginUserMylistItemIdRepository _loginUserMylistItemIdRepository;

        public LoginUserMylistProvider(
            NiconicoSession niconicoSession,
            NicoVideoCacheRepository nicoVideoRepository,
            LoginUserMylistItemIdRepository loginUserMylistItemIdRepository
            )
            : base(niconicoSession)
        {
            _nicoVideoRepository = nicoVideoRepository;
            _loginUserMylistItemIdRepository = loginUserMylistItemIdRepository;            
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
                Label = "WatchAfterMylist".Translate(),
                Count = (int)defMylist.Data.Mylist.TotalItemCount,
                UserId = NiconicoSession.UserIdString,
                ThumbnailImages = defMylist.Data.Mylist.Items.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray(),
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
                    Count = (int)mylistGroup.ItemsCount,
                    UserId = mylistGroup.Owner.Id,
                    Description = mylistGroup.Description,
                    IsPublic = mylistGroup.IsPublic,
                    //IconType = mylistGroup.co,
                    DefaultSortKey = mylistGroup.DefaultSortKey,
                    DefaultSortOrder = mylistGroup.DefaultSortOrder,
                    SortIndex = res.Data.Mylists.IndexOf(mylistGroup),
                    ThumbnailImages = mylistGroup.SampleItems.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray(),
                };

                mylistGroups.Add(mylist);
            }

            return mylistGroups;
        }

        public async Task<List<NicoVideo>> GetLoginUserMylistItemsAsync(IMylist mylist, MylistSortKey sortKey, MylistSortOrder sortOrder, uint pageSize, uint page)
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

                foreach (var item in items)
                {
                    _loginUserMylistItemIdRepository.AddItem(item.ItemId.ToString(), mylist.Id, item.WatchId);
                }

                return items.Select(x => MylistDataToNicoVideoData(x)).Cast<NicoVideo>().ToList();

            }
            else
            {
                var res = await ContextActionAsync(async context =>
                {
                    var mylistItemsRes = await context.User.GetLoginUserMylistGroupItemsAsync(int.Parse(mylist.Id), sortKey, sortOrder, pageSize, page);
                    return mylistItemsRes.Data.Mylist;
                });

                var items = res.Items;
                foreach (var item in items)
                {
                    _loginUserMylistItemIdRepository.AddItem(item.ItemId.ToString(), mylist.Id, item.WatchId);
                }

                return items.Select(x => MylistDataToNicoVideoData(x)).ToList();
            }
        }


        static public bool IsDefaultMylist(IMylist mylist)
        {
            return mylist?.Id == "0";
        }


        private NicoVideo MylistDataToNicoVideoData(Item item)
        {
            var video = _nicoVideoRepository.Get(item.WatchId)
                        ?? new NicoVideo() { RawVideoId = item.WatchId };

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

            video.Owner = item.Video.Owner.Id == null ? null : new NicoVideoOwner()
            {
                OwnerId = item.Video.Owner.Id,
                ScreenName = item.Video.Owner.Name,
                UserType = item.Video.Owner.OwnerType switch
                {
                    OwnerType.Channel => NicoVideoUserType.Channel,
                    OwnerType.Hidden => NicoVideoUserType.Hidden,
                    OwnerType.User => NicoVideoUserType.User,
                    _ => throw new NotSupportedException(),
                },
                IconUrl = item.Video.Owner.IconUrl?.OriginalString,
            };

            _nicoVideoRepository.AddOrUpdate(video);

            return video;
        }


        public async Task<string> AddMylist(string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.User.CreateMylistGroupAsync(name, description, isPublic, sortKey, sortOrder);
            });
        }

        public async Task<bool> UpdateMylist(string mylistId, string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.User.UpdateMylistGroupAsync(mylistId, name, description, isPublic, sortKey, sortOrder);
            });
        }


        public async Task<bool> RemoveMylist(string group_id)
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
            var itemId = _loginUserMylistItemIdRepository.GetItemId(mylistGroupid, videoId);
            if (itemId == null) { return ContentManageResult.Failed; }
            return await ContextActionAsync(async context =>
            {
                return await context.User.RemoveMylistItemAsync(mylistGroupid, itemId);
            });
        }

        public async Task<ContentManageResult> CopyMylistTo(string sourceMylistGroupId, string targetGroupId, params string[] videoIdList)
        {
            var items = videoIdList.Select(x => _loginUserMylistItemIdRepository.GetItemId(sourceMylistGroupId, x));
            return await ContextActionAsync(async context =>
            {
                return await context.User.CopyMylistItemAsync(sourceMylistGroupId, targetGroupId, items.ToArray());
            });
        }


        public async Task<ContentManageResult> MoveMylistTo(string sourceMylistGroupId, string targetGroupId, params string[] videoIdList)
        {
            var items = videoIdList.Select(x => _loginUserMylistItemIdRepository.GetItemId(sourceMylistGroupId, x));
            return await ContextActionAsync(async context =>
            {
                return await context.User.MoveMylistItemAsync(sourceMylistGroupId, targetGroupId, items.ToArray());
            });
        }
    }

}
