
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using I18NPortable;
using Hohoema.Models.Domain.Niconico.User;

namespace Hohoema.Models.Domain.Niconico.LoginUser.Mylist
{
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
                    Label = detail.Name ?? "WatchAfterMylist".Translate(),
                    Count = (int)detail.TotalItemCount,
                    IsPublic = true,
                    ThumbnailImages = detail.SampleItems?.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray(),
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
                    ThumbnailImages = detail.SampleItems?.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray(),
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
    }
}
