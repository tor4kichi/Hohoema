using Mntone.Nico2.Mylist;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    public class OtherOwneredMylistManager
    {
        public readonly Dictionary<string, OtherOwneredMylist> CachedMylist = new Dictionary<string, OtherOwneredMylist>();


        NiconicoContentProvider NiconicoContentFinder { get; }

        public OtherOwneredMylistManager(NiconicoContentProvider contentFinder)
        {
            NiconicoContentFinder = contentFinder;

        }

        public OtherOwneredMylist MakeAndRegistrationCache(string groupId, string name, int count, int sortIndex)
        {
            return new OtherOwneredMylist(groupId, name, count, sortIndex);
        }

        public OtherOwneredMylist MakeAndRegistrationCache(MylistGroupData data, int sortIndex)
        {
            return new OtherOwneredMylist(data.Id, data.Name, data.Count, sortIndex);
        }

        public async Task<OtherOwneredMylist> GetMylist(string mylistGroupId)
        {
            if (CachedMylist.ContainsKey(mylistGroupId))
            {
                return CachedMylist[mylistGroupId];
            }
            else
            {
                var res = await NiconicoContentFinder.GetMylistGroupDetail(mylistGroupId);
                if (res.IsOK)
                {
                    var detail = res.MylistGroup;
                    return MakeAndRegistrationCache(detail.Id, detail.Name, (int)detail.Count, 0);
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task<List<OtherOwneredMylist>> GetByUserId(string userId)
        {
            var groups = await NiconicoContentFinder.GetUserMylistGroups(userId);
            if (groups == null ) { return null; }

            var list = groups.Select((x, i) => MakeAndRegistrationCache(x, i)).ToList();

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



    public sealed class OtherOwneredMylist : IPlayableList
    {
        public PlaylistOrigin Origin => PlaylistOrigin.OtherUser;

        public string Id { get; }

        public int SortIndex { get; }

        public string Name { get; }

        private ObservableCollection<PlaylistItem> _PlaylistItems;
        public ReadOnlyObservableCollection<PlaylistItem> PlaylistItems { get; }


        public int Count { get; }

        public OtherOwneredMylist(string groupId, string name, int count, int sortIndex = 0)
        {
            Id = groupId;
            Name = name;
            SortIndex = sortIndex;
            Count = count;

            _PlaylistItems = new ObservableCollection<PlaylistItem>();
            PlaylistItems = new ReadOnlyObservableCollection<PlaylistItem>(_PlaylistItems);
        }
    }
}
