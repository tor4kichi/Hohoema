using Hohoema.Models.Domain.Niconico.Video;
using NiconicoToolkit.Mylist;
using System.Collections.Generic;

namespace Hohoema.Models.Domain.Niconico.Mylist
{
    public class MylistItemsGetResult
    {
        public bool IsSuccess { get; set; }
        public IMylist Mylist { get; set; }
        public bool IsLoginUserMylist { get; set; }
        public bool IsDefaultMylist { get; set; }
        public int ItemsHeadPosition { get; set; }
        public int TotalCount { get; set; }
        public IReadOnlyCollection<NicoVideo> NicoVideoItems { get; set; }

        public int Count => NicoVideoItems.Count;

        public IReadOnlyCollection<MylistItem> Items { get; internal set; }
    }
}
