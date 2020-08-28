using Mntone.Nico2.Mylist.MylistGroup;
using Mntone.Nico2.Users.Mylist;
using NicoPlayerHohoema.Database;
using NicoPlayerHohoema.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Provider
{
    
    public sealed class MylistProvider : ProviderBase
    {
        public MylistProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public class MylistItemsGetResult
        {
            public bool IsSuccess { get; set; }

            public string MylistId { get; set; }

            public int HeadPosition { get; set; }
            public int TotalCount { get; set; }

            public IReadOnlyCollection<NicoVideo> Items { get; set; }
        }



        public async Task<Mylist> GetMylistGroupDetail(string mylistGroupid)
        {
            if (mylistGroupid == "0")
            {
                var res = await ContextActionAsync(async context =>
                {
                    return await context.User.GetWatchAfterMylistGroupItemsAsync(Mntone.Nico2.Users.Mylist.MylistSortKey.AddedAt, Mntone.Nico2.Users.Mylist.MylistSortOrder.Asc, 1, 0);
                });

                return new Mylist()
                {
                    Id = int.Parse(mylistGroupid),
                    ItemsCount = res.Data.Mylist.TotalItemCount,
                };
            }
            else
            {
                var res = await ContextActionAsync(async context =>
                {
                    return await context.User.GetMylistGroupItemsAsync(int.Parse(mylistGroupid), Mntone.Nico2.Users.Mylist.MylistSortKey.AddedAt, Mntone.Nico2.Users.Mylist.MylistSortOrder.Asc, 1, 0);
                });

                if (res.Meta.Status != 200)
                {
                    res = await ContextActionAsync(async context =>
                    {
                        return await context.User.GetLoginUserMylistGroupItemsAsync(int.Parse(mylistGroupid), Mntone.Nico2.Users.Mylist.MylistSortKey.AddedAt, Mntone.Nico2.Users.Mylist.MylistSortOrder.Asc, 1, 0);
                    });
                }

                return res.Data.Mylist;
            }
        }




        public async Task<MylistItemsGetResult> GetMylistGroupVideo(string mylistId, MylistSortKey sortKey, MylistSortOrder sortOrder, uint pageSize, uint page)
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
                var nicoVideo = Database.NicoVideoDb.Get(item.Video.Id);
                nicoVideo.Title = item.Video.Title;
                nicoVideo.ThumbnailUrl = item.Video.Thumbnail.ListingUrl.OriginalString;
                nicoVideo.PostedAt = item.Video.RegisteredAt.DateTime;
                nicoVideo.Length = TimeSpan.FromSeconds(item.Video.Duration);
                nicoVideo.IsDeleted = item.IsDeleted;
                nicoVideo.DescriptionWithHtml = item.Description;
                nicoVideo.MylistCount = (int)item.Video.Count.Mylist;
                nicoVideo.CommentCount = (int)item.Video.Count.Comment;
                nicoVideo.ViewCount = (int)item.Video.Count.View;

                nicoVideo.Owner = new Database.NicoVideoOwner()
                {
                    OwnerId = item.Video.Owner.Id,
                    UserType = item.Video.Owner.OwnerType switch
                    {
                        OwnerType.Channel => NicoVideoUserType.Channel,
                        OwnerType.Hidden => NicoVideoUserType.Hidden,
                        OwnerType.User => NicoVideoUserType.User,
                        _ => throw new NotSupportedException(),
                    }
                };

                Database.NicoVideoDb.AddOrUpdate(nicoVideo);

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

        //public async Task<OtherOwneredMylist> GetMylistGroupVideo(string mylistGroupId, uint from = 0, uint limit = 50)
        //{
        //    var detail = await GetMylistGroupDetail(mylistGroupId);

        //    if (!detail.IsOK) { return null; }

        //    var res = await ContextActionAsync(async context =>
        //    {
        //        return await context.Mylist.GetMylistGroupVideoAsync(mylistGroupId, from, limit);
        //    });
        //    if (!res.IsOK) { return null; }

        //    var mylistInfo = detail.MylistGroup;
        //    var videos = res.MylistVideoInfoItems;
        //    var mylist = new OtherOwneredMylist()
        //    {
        //        Id = mylistGroupId,
        //        Label = mylistInfo.Name,
        //        Description = mylistInfo.Description,
        //        UserId = mylistInfo.UserId,
        //        ItemCount = (int)mylistInfo.Count,
        //    };

        //    foreach (var item in videos)
        //    {
        //        var nicoVideo = Database.NicoVideoDb.Get(item.Video.Id);
        //        nicoVideo.Title = item.Video.Title;
        //        nicoVideo.ThumbnailUrl = item.Video.ThumbnailUrl.OriginalString;
        //        nicoVideo.PostedAt = item.Video.FirstRetrieve;
        //        nicoVideo.Length = item.Video.Length;
        //        nicoVideo.IsDeleted = item.Video.IsDeleted;
        //        nicoVideo.DescriptionWithHtml = item.Video.Description;
        //        nicoVideo.MylistCount = (int)item.Video.MylistCount;
        //        nicoVideo.CommentCount = (int)item.Thread.GetCommentCount();
        //        nicoVideo.ViewCount = (int)item.Video.ViewCount;

        //        Database.NicoVideoDb.AddOrUpdate(nicoVideo);

        //        mylist.Add(item.Video.Id);
        //    }

        //    return mylist;
        //}

        //public async Task<OtherOwneredMylist> GetMylistGroupVideo(string mylistGroupId)
        //{
        //    var detail = await GetMylistGroupDetail(mylistGroupId);
             
        //    if (!detail.IsOK) { return null; }

        //    var mylistInfo = detail.MylistGroup;
        //    var mylist = new OtherOwneredMylist()
        //    {
        //        Id = mylistGroupId,
        //        Label = mylistInfo.Name,
        //        Description = mylistInfo.Description,
        //        UserId = mylistInfo.UserId,
        //        ItemCount = (int)mylistInfo.Count,
        //    };

        //    var count = (int)detail.MylistGroup.Count;

        //    var tryCount = (count / 150) + 1;
        //    foreach (var index in Enumerable.Range(0, tryCount))
        //    {
        //        if (index != 0)
        //        {
        //            await Task.Delay(500);
        //        }

        //        var result = await ContextActionAsync(async context =>
        //        {
        //            return await context.Mylist.GetMylistGroupVideoAsync(mylistGroupId, (uint)index * 150, 150);
        //        });                

        //        if (!result.IsOK) { break; }

        //        foreach (var item in result.MylistVideoInfoItems)
        //        {
        //            var nicoVideo = Database.NicoVideoDb.Get(item.Video.Id);
        //            nicoVideo.Title = item.Video.Title;
        //            nicoVideo.ThumbnailUrl = item.Video.ThumbnailUrl.OriginalString;
        //            nicoVideo.PostedAt = item.Video.FirstRetrieve;
        //            nicoVideo.Length = item.Video.Length;
        //            nicoVideo.IsDeleted = item.Video.IsDeleted;
        //            nicoVideo.DescriptionWithHtml = item.Video.Description;
        //            nicoVideo.MylistCount = (int)item.Video.MylistCount;
        //            nicoVideo.CommentCount = (int)item.Thread.GetCommentCount();
        //            nicoVideo.ViewCount = (int)item.Video.ViewCount;

        //            Database.NicoVideoDb.AddOrUpdate(nicoVideo);

        //            mylist.Add(item.Video.Id);
        //        }
        //    }

        //    return mylist;
        //}

        //public async Task FillMylistGroupVideo(OtherOwneredMylist mylist)
        //{
        //    if (mylist.IsFilled) { return; }

        //    if (mylist.ItemCount == 0)
        //    {
        //        throw new NotSupportedException();
        //    }

        //    var count = (int)mylist.ItemCount;

        //    var tryCount = (count / 150) + 1;
        //    var increaseCount = mylist.Count;
        //    foreach (var index in Enumerable.Range(0, tryCount))
        //    {
        //        if (index != 0)
        //        {
        //            await Task.Delay(500);
        //        }

        //        var result = await ContextActionAsync(async context =>
        //        {
        //            return await context.Mylist.GetMylistGroupVideoAsync(
        //                group_id: mylist.Id,
        //                from: (uint)(index * 150 + increaseCount),
        //                limit: 150
        //                );
        //        });
        //        if (!result.IsOK) { break; }

        //        foreach (var item in result.MylistVideoInfoItems)
        //        {
        //            var nicoVideo = Database.NicoVideoDb.Get(item.Video.Id);
        //            nicoVideo.Title = item.Video.Title;
        //            nicoVideo.ThumbnailUrl = item.Video.ThumbnailUrl.OriginalString;
        //            nicoVideo.PostedAt = item.Video.FirstRetrieve;
        //            nicoVideo.Length = item.Video.Length;
        //            nicoVideo.IsDeleted = item.Video.IsDeleted;
        //            nicoVideo.DescriptionWithHtml = item.Video.Description;
        //            nicoVideo.MylistCount = (int)item.Video.MylistCount;
        //            nicoVideo.CommentCount = (int)item.Thread.GetCommentCount();
        //            nicoVideo.ViewCount = (int)item.Video.ViewCount;

        //            Database.NicoVideoDb.AddOrUpdate(nicoVideo);

        //            mylist.Add(item.Video.Id);
        //        }
        //    }

        //    return;
        //}
    }
}
