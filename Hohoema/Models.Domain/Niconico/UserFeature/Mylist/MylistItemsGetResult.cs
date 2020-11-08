using Hohoema.Models.Domain.Niconico.Video;
using System.Collections.Generic;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;

namespace Hohoema.Models.Domain.Niconico.UserFeature.Mylist
{
    public class MylistItemsGetResult
    {
        public bool IsSuccess { get; set; }
        public IMylist Mylist { get; set; }
        public bool IsLoginUserMylist { get; set; }
        public bool IsDefaultMylist { get; set; }
        public int ItemsHeadPosition { get; set; }
        public int TotalCount { get; set; }
        public IReadOnlyCollection<NicoVideo> Items { get; set; }

        public int Count => Items.Count;
    }
}
