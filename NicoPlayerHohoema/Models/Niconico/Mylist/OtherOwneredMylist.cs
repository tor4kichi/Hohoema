using Mntone.Nico2.Mylist;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Hohoema.NicoAlert.Helpers;
using Mntone.Nico2.Mylist.MylistGroup;
using Mntone.Nico2.Searches.Mylist;
using System.Windows.Input;
using Prism.Commands;

namespace NicoPlayerHohoema.Models
{
    public class OtherOwneredMylistManager
    {
        public OtherOwneredMylistManager(
            Models.Provider.MylistProvider mylistProvider,
            Provider.UserProvider userProvider
            )
        {
            MylistProvider = mylistProvider;
            UserProvider = userProvider;
        }

        public readonly Dictionary<string, OtherOwneredMylist> CachedMylist = new Dictionary<string, OtherOwneredMylist>();

        public Provider.MylistProvider MylistProvider { get; }
        public Provider.UserProvider UserProvider { get; }

        public async Task<OtherOwneredMylist> GetMylist(string mylistGroupId)
        {
            if (CachedMylist.ContainsKey(mylistGroupId))
            {
                return CachedMylist[mylistGroupId];
            }
            else
            {
                var res = await MylistProvider.GetMylistGroupDetail(mylistGroupId);
                if (res.IsOK)
                {
                    var detail = res.MylistGroup;
                    var mylist = new OtherOwneredMylist()
                    {
                        Id = detail.Id,
                        SortIndex = 0,
                        Label = detail.Name,
                        UserId = detail.UserId,
                        Description = detail.Description,
                        ItemCount = (int)detail.Count,
                    };
                    CachedMylist.Add(mylistGroupId, mylist);
                    return mylist;
                }
                else
                {
                    return null;
                }
            }
        }

        public OtherOwneredMylist GetMylistIfCached(string mylistGroupId)
        {
            if (CachedMylist.ContainsKey(mylistGroupId))
            {
                return CachedMylist[mylistGroupId];
            }
            else
            {
                return null;
            }
        }


        public async Task<List<OtherOwneredMylist>> GetByUserId(string userId)
        {
            var groups = await UserProvider.GetUserMylistGroups(userId);
            if (groups == null ) { return null; }

            var list = groups.Select((x, i) => 
            {
                return new OtherOwneredMylist()
                {
                    Id = x.Id,
                    SortIndex = i,
                    Label = x.Name,
                    UserId = x.UserId,
                    Description = x.Description,
                    ItemCount = x.Count,
                };
            }
            ).ToList();

            foreach (var item in list)
            {
                if (!CachedMylist.ContainsKey(item.Id))
                {
                    CachedMylist.Add(item.Id, item);
                }
            }

            return list;
        }
    }



    public sealed class OtherOwneredMylist : Collection<string>, Interfaces.IOtherOwnedMylist
    {
        public OtherOwneredMylist() { }

        public OtherOwneredMylist(IList<string> list) : base(list) { }


        public int ItemCount { get; internal set; }
        public string Id { get; internal set; }
        public int SortIndex { get; internal set; }
        public string Label { get; internal set; }
        public string Description { get; internal set; }
        public string UserId { get; internal set; }


        public bool IsFilled => ItemCount == this.Count;
    }
}
