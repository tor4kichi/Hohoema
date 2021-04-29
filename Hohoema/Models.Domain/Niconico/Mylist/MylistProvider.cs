using Mntone.Nico2.Mylist.MylistGroup;
using Mntone.Nico2.Users.Mylist;
using Hohoema.Database;

using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using I18NPortable;

namespace Hohoema.Models.Domain.Niconico.Mylist
{
    
    public sealed class MylistProvider : ProviderBase
    {
        private readonly NicoVideoCacheRepository _nicoVideoRepository;

        public MylistProvider(NiconicoSession niconicoSession,
            NicoVideoCacheRepository nicoVideoRepository
            )
            : base(niconicoSession)
        {
            _nicoVideoRepository = nicoVideoRepository;
        }

        public class MylistItemsGetResult
        {
            public bool IsSuccess { get; set; }

            public string MylistId { get; set; }

            public int HeadPosition { get; set; }
            public int TotalCount { get; set; }

            public IReadOnlyCollection<NicoVideo> Items { get; set; }
        }



        private async Task<Mntone.Nico2.Users.Mylist.Mylist> GetMylistGroupDetail(string mylistGroupid)
        {
            if (mylistGroupid == "0")
            {
                var res = await ContextActionAsync(context =>
                {
                    return context.User.GetWatchAfterMylistGroupItemsAsync(Mntone.Nico2.Users.Mylist.MylistSortKey.AddedAt, Mntone.Nico2.Users.Mylist.MylistSortOrder.Asc, 1, 0);
                });

                return new Mntone.Nico2.Users.Mylist.Mylist()
                {
                    Id = int.Parse(mylistGroupid),
                    ItemsCount = res.Data.Mylist.TotalItemCount,
                };
            }
            else
            {
                var res = await ContextActionAsync(context =>
                {
                    return context.User.GetMylistGroupItemsAsync(int.Parse(mylistGroupid), Mntone.Nico2.Users.Mylist.MylistSortKey.AddedAt, Mntone.Nico2.Users.Mylist.MylistSortOrder.Asc, 2, 0);
                });

                if (res.Meta.Status != 200)
                {
                    res = await ContextActionAsync(context =>
                    {
                        return context.User.GetLoginUserMylistGroupItemsAsync(int.Parse(mylistGroupid), Mntone.Nico2.Users.Mylist.MylistSortKey.AddedAt, Mntone.Nico2.Users.Mylist.MylistSortOrder.Asc, 1, 0);
                    });
                }

                return res.Data.Mylist;
            }
        }

        public async Task<MylistPlaylist> GetMylist(string mylistGroupId)
        {
            var detail = await GetMylistGroupDetail(mylistGroupId);

            if (mylistGroupId == "0")
            {
                var mylist = new MylistPlaylist(detail.Id.ToString(), this)
                {
                    Label = detail.Name ?? "WatchAfterMylist".Translate(),
                    Count = (int)detail.TotalItemCount,
                    IsPublic = true,
                    ThumbnailImages = detail.SampleItems?.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray(),
                };
                return mylist;
            }
            else
            {
                var mylist = new MylistPlaylist(detail.Id.ToString(), this)
                {
                    Label = detail.Name,
                    Count = (int)detail.TotalItemCount,
                    CreateTime = detail.CreatedAt.DateTime,
                    //DefaultSortOrder = ,
                    IsPublic = detail.IsPublic,
                    SortIndex = 0,
                    UserId = detail.Owner.Id,
                    Description = detail.Description,
                    ThumbnailImages = detail.SampleItems?.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray(),
                };
                return mylist;
            }
        }



        public async Task<List<MylistPlaylist>> GetMylistsByUser(string userId)
        {
            var groups = await ContextActionWithPageAccessWaitAsync(context =>
            {
                return context.User.GetMylistGroupsAsync(int.Parse(userId));
            });

            if (groups == null) { return null; }

            var list = groups.Data.Mylists.Select((x, i) =>
            {
                return new MylistPlaylist(x.Id.ToString(), this)
                {
                    Label = x.Name,
                    Count = (int)(x.ItemsCount == 0 ? x.TotalItemCount : x.ItemsCount),
                    SortIndex = i,
                    UserId = x.Owner.Id,
                    Description = x.Description,
                    IsPublic = x.IsPublic,
                    CreateTime = x.CreatedAt.DateTime,
                    DefaultSortKey = x.DefaultSortKey,
                    DefaultSortOrder = x.DefaultSortOrder,
                    ThumbnailImages = x.SampleItems.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray()
                };
            }
            ).ToList();

            return list;
        }



        public async Task<MylistItemsGetResult> GetMylistVideoItems(string mylistId, MylistSortKey sortKey, MylistSortOrder sortOrder, uint pageSize, uint page)
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.User.GetMylistGroupItemsAsync(int.Parse(mylistId), sortKey, sortOrder, pageSize, page);
            });
            
            if (res.Meta.Status != 200) { return new MylistItemsGetResult() { IsSuccess = false, MylistId = mylistId }; }

            var videos = res.Data.Mylist.Items;
            var resultItems = new List<NicoVideo>();
            foreach (var item in videos)
            {
                var nicoVideo = _nicoVideoRepository.Get(item.Video.Id);
                nicoVideo.Title = item.Video.Title;
                nicoVideo.ThumbnailUrl = item.Video.Thumbnail.ListingUrl.OriginalString;
                nicoVideo.PostedAt = item.Video.RegisteredAt.DateTime;
                nicoVideo.Length = TimeSpan.FromSeconds(item.Video.Duration);
                nicoVideo.IsDeleted = item.IsDeleted;
                nicoVideo.DescriptionWithHtml = item.Description;
                nicoVideo.MylistCount = (int)item.Video.Count.Mylist;
                nicoVideo.CommentCount = (int)item.Video.Count.Comment;
                nicoVideo.ViewCount = (int)item.Video.Count.View;

                nicoVideo.Owner = new NicoVideoOwner()
                {
                    OwnerId = item.Video.Owner.Id ,
                    UserType = item.Video.Owner.OwnerType switch
                    {
                        OwnerType.Channel => NicoVideoUserType.Channel,
                        OwnerType.Hidden => NicoVideoUserType.Hidden,
                        OwnerType.User => NicoVideoUserType.User,
                        _ => throw new NotSupportedException(),
                    }
                };

                // OwnerType.Hiddenだった場合にOwnerIdのNull例外が発生するのでオーナー情報を削除しておく
                if (nicoVideo.Owner.UserType is NicoVideoUserType.Hidden)
                {
                    nicoVideo.Owner = null;
                }

                _nicoVideoRepository.AddOrUpdate(nicoVideo);

                resultItems.Add(nicoVideo);
            }

            return new MylistItemsGetResult()
            {
                IsSuccess = true,
                MylistId = mylistId,
                HeadPosition = (int)(pageSize * page),
                Items = new ReadOnlyCollection<NicoVideo>(resultItems),
                TotalCount = (int)res.Data.Mylist.TotalItemCount,
            };
        }

    }
}
