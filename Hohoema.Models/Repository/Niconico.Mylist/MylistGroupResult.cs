using Hohoema.Database;
using Hohoema.Models.Repository.Niconico;
using Mntone.Nico2.Mylist.MylistGroup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Mylist
{
    internal sealed class MylistGroupResult : IMylistGroupResult
    {
        private readonly MylistGroupDetailResponse _res;

        public MylistGroupResult(MylistGroupDetailResponse res)
        {
            _res = res;


            // TODO: MylistGroupのサンプル動画のMapping
            SampleVideos = new List<Database.NicoVideo>();
        }

        public bool IsOK => _res.IsOK;

        public string Id => _res.MylistGroup?.Id;

        public string UserId => _res.MylistGroup.UserId;

        public int ViewCount => (int)_res.MylistGroup.ViewCount;

        public string Name => _res.MylistGroup?.Name;

        public string Description => _res.MylistGroup.Description;

        public bool IsPublic => _res.MylistGroup.IsPublic;

        public MylistGroupDefaultSort DefaultSort => _res.MylistGroup.GetMylistDefaultSort().ToModelDefaultSort();

        public MylistGroupIconType IconType => _res.MylistGroup.GetIconType().ToModelIconType();

        public Order Order => _res.MylistGroup.GetSortOrder().ToModelOrder();

        public DateTime UpdateTime => _res.MylistGroup.UpdateTime;

        public DateTime CreateTime => _res.MylistGroup.CreateTime;

        public int ItemsCount => (int)_res.MylistGroup.Count;

        public IList<Database.NicoVideo> SampleVideos { get; }
    }
}
