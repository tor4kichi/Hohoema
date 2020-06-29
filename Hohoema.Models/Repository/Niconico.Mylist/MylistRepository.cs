using Hohoema.Interfaces;
using Hohoema.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Repository;
using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.Models.Repository.Niconico;

namespace Hohoema.Models.Repository.Niconico.Mylist
{
    

    

   


    public static class MylistPlaylistExtension
    {
        public const string DefailtMylistId = "0";

        public static bool IsDefaultMylist(this IMylist mylist)
        {
            return mylist?.Id == DefailtMylistId;
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
                var mylist = new MylistPlaylist(res.Id, MylistProvider)
                {
                    Label = res.Name,
                    Count = res.ItemsCount,
                    IconType = res.IconType,
                    CreateTime = res.CreateTime,
                    UpdateTime = res.UpdateTime,
                    Order = res.Order,
                    IsPublic = res.IsPublic,
                    SortIndex = 0,
                    UserId = res.UserId,
                    Description = res.Description,
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
                return new MylistPlaylist(x.Id, MylistProvider)
                {
                    Label = x.Name,
                    Count = x.ItemsCount,
                    SortIndex = i,
                    UserId = x.UserId,
                    Description = x.Description,
                    IsPublic = x.IsPublic,
                    IconType = x.IconType,
                };
            }
            ).ToList();

            return list;
        }
    }
}
