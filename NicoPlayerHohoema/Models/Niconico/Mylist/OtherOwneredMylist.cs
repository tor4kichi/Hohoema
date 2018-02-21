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


        public OtherOwneredMylist MakeAndRegistrationCache(MylistGroupData data, int sortIndex)
        {
            return new OtherOwneredMylist(data, sortIndex);
        }

        private OtherOwneredMylist MakeAndRegistrationCache(Mylistgroup mylistGroup)
        {
            return new OtherOwneredMylist(mylistGroup);
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
                    var mylist = MakeAndRegistrationCache(res.MylistGroup);
                    CachedMylist.Add(mylistGroupId, mylist);
                    return mylist;
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
        public int Count { get; }
        public string Description { get; }
        public string OwnerUserId { get; }

        private ObservableCollection<PlaylistItem> _PlaylistItems;
        public ReadOnlyObservableCollection<PlaylistItem> PlaylistItems { get; }



        AsyncLock _FillVideoLock = new AsyncLock();

        public OtherOwneredMylist()
        {
            _PlaylistItems = new ObservableCollection<PlaylistItem>();
            PlaylistItems = new ReadOnlyObservableCollection<PlaylistItem>(_PlaylistItems);
        }

        public OtherOwneredMylist(Mylistgroup details)
            : this()
        {
            Id = details.Id;
            Name = details.Name;
            SortIndex = (int)details.GetSortOrder();
            Count = (int)details.Count;
            Description = details.Description;
            OwnerUserId = details.UserId;
        }

        public OtherOwneredMylist(MylistGroupData data, int sortIndex = 0)
            : this()
        {
            Id = data.Id;
            Name = data.Name;
            SortIndex = sortIndex;
            Count = (int)data.Count;
            Description = data.Description;
            SortIndex = sortIndex;
            OwnerUserId = data.UserId;
        }

        public async Task<bool> FillAllVideosAsync()
        {
            using (var releaser = await _FillVideoLock.LockAsync())
            {
                if (_PlaylistItems.Count == Count)
                {
                    return true;
                }

                var contentManager = App.Current.Container.Resolve<NiconicoContentProvider>();
                var tryCount = (Count / 150) + 1;
                foreach (var index in Enumerable.Range(0, tryCount))
                {
                    var result = await contentManager.GetMylistGroupVideo(Id, (uint)index * 150, 150);

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

                        _PlaylistItems.Add(new PlaylistItem()
                        {
                            Owner = this,
                            Title = item.Video.Title,
                            ContentId = item.Video.Id,
                            Type = PlaylistItemType.Video,
                        });
                    }

                    await Task.Delay(500);
                }
            }

            return _PlaylistItems.Count == Count;
        }
    }
}
