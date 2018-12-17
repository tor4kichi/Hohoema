using Mntone.Nico2.Mylist.MylistGroup;
using System;
using System.Collections.Generic;
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


        public async Task<MylistGroupDetailResponse> GetMylistGroupDetail(string mylistGroupid)
        {
            return await Context.Mylist.GetMylistGroupDetailAsync(mylistGroupid);
        }

        public async Task<OtherOwneredMylist> GetMylistGroupVideo(string mylistGroupid, uint from = 0, uint limit = 50)
        {
            var detail = await GetMylistGroupDetail(mylistGroupid);
            if (!detail.IsOK) { return null; }

            var res = await NiconicoSession.Context.Mylist.GetMylistGroupVideoAsync(mylistGroupid, from, limit);
            if (!res.IsOK) { return null; }

            var mylistInfo = detail.MylistGroup;
            var videos = res.MylistVideoInfoItems;
            var mylist = new OtherOwneredMylist()
            {
                Id = mylistGroupid,
                Label = mylistInfo.Name,
                Description = mylistInfo.Description,
                UserId = mylistInfo.UserId,
                ItemCount = (int)mylistInfo.Count,
            };

            foreach (var item in videos)
            {
                var nicoVideo = Database.NicoVideoDb.Get(item.Video.Id);
                nicoVideo.Title = item.Video.Title;
                nicoVideo.ThumbnailUrl = item.Video.ThumbnailUrl.OriginalString;
                nicoVideo.PostedAt = item.Video.FirstRetrieve;
                nicoVideo.Length = item.Video.Length;
                nicoVideo.IsDeleted = item.Video.IsDeleted;
                nicoVideo.DescriptionWithHtml = item.Video.Description;
                nicoVideo.MylistCount = (int)item.Video.MylistCount;
                nicoVideo.CommentCount = (int)item.Thread.GetCommentCount();
                nicoVideo.ViewCount = (int)item.Video.ViewCount;

                Database.NicoVideoDb.AddOrUpdate(nicoVideo);

                mylist.Add(item.Video.Id);
            }

            return mylist;
        }

        public async Task<OtherOwneredMylist> GetMylistGroupVideo(string mylistGroupid)
        {
            var detail = await GetMylistGroupDetail(mylistGroupid);
            if (!detail.IsOK) { return null; }

            var mylistInfo = detail.MylistGroup;
            var mylist = new OtherOwneredMylist()
            {
                Id = mylistGroupid,
                Label = mylistInfo.Name,
                Description = mylistInfo.Description,
                UserId = mylistInfo.UserId,
                ItemCount = (int)mylistInfo.Count,
            };

            var count = (int)detail.MylistGroup.Count;

            var tryCount = (count / 150) + 1;
            foreach (var index in Enumerable.Range(0, tryCount))
            {
                if (index != 0)
                {
                    await Task.Delay(500);
                }

                var result = await NiconicoSession.Context.Mylist.GetMylistGroupVideoAsync(mylistGroupid, (uint)index * 150, 150);

                if (!result.IsOK) { break; }

                foreach (var item in result.MylistVideoInfoItems)
                {
                    var nicoVideo = Database.NicoVideoDb.Get(item.Video.Id);
                    nicoVideo.Title = item.Video.Title;
                    nicoVideo.ThumbnailUrl = item.Video.ThumbnailUrl.OriginalString;
                    nicoVideo.PostedAt = item.Video.FirstRetrieve;
                    nicoVideo.Length = item.Video.Length;
                    nicoVideo.IsDeleted = item.Video.IsDeleted;
                    nicoVideo.DescriptionWithHtml = item.Video.Description;
                    nicoVideo.MylistCount = (int)item.Video.MylistCount;
                    nicoVideo.CommentCount = (int)item.Thread.GetCommentCount();
                    nicoVideo.ViewCount = (int)item.Video.ViewCount;

                    Database.NicoVideoDb.AddOrUpdate(nicoVideo);

                    mylist.Add(item.Video.Id);
                }
            }

            return mylist;
        }

        public async Task FillMylistGroupVideo(OtherOwneredMylist mylist)
        {
            if (mylist.IsFilled) { return; }

            if (mylist.ItemCount == 0)
            {
                throw new NotSupportedException();
            }

            var count = (int)mylist.ItemCount;

            var tryCount = (count / 150) + 1;
            var increaseCount = mylist.Count;
            foreach (var index in Enumerable.Range(0, tryCount))
            {
                if (index != 0)
                {
                    await Task.Delay(500);
                }

                var result = await NiconicoSession.Context.Mylist.GetMylistGroupVideoAsync(
                    group_id: mylist.Id, 
                    from: (uint)(index * 150 + increaseCount), 
                    limit: 150
                    );

                if (!result.IsOK) { break; }

                foreach (var item in result.MylistVideoInfoItems)
                {
                    var nicoVideo = Database.NicoVideoDb.Get(item.Video.Id);
                    nicoVideo.Title = item.Video.Title;
                    nicoVideo.ThumbnailUrl = item.Video.ThumbnailUrl.OriginalString;
                    nicoVideo.PostedAt = item.Video.FirstRetrieve;
                    nicoVideo.Length = item.Video.Length;
                    nicoVideo.IsDeleted = item.Video.IsDeleted;
                    nicoVideo.DescriptionWithHtml = item.Video.Description;
                    nicoVideo.MylistCount = (int)item.Video.MylistCount;
                    nicoVideo.CommentCount = (int)item.Thread.GetCommentCount();
                    nicoVideo.ViewCount = (int)item.Video.ViewCount;

                    Database.NicoVideoDb.AddOrUpdate(nicoVideo);

                    mylist.Add(item.Video.Id);
                }
            }

            return;
        }
    }
}
