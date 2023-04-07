using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Mylist;
using System.Collections.Generic;

namespace Hohoema.Models.Niconico.Mylist
{
    public class MylistItemsGetResult
    {
        public bool IsSuccess { get; set; }

        public MylistId MylistId { get; set; }

        public int HeadPosition { get; set; }
        public int TotalCount { get; set; }

        public IReadOnlyCollection<MylistItem> Items { get; set; }
        public IReadOnlyCollection<NicoVideo> NicoVideoItems { get; set; }
    }
}
