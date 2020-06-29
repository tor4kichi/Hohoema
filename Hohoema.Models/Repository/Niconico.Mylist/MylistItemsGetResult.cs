using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Mylist
{
    public class MylistItemsGetResult
    {
        public bool IsSuccess { get; set; }
        public IMylist Mylist { get; set; }
        public bool IsLoginUserMylist { get; set; }
        public bool IsDefaultMylist { get; set; }
        public int ItemsHeadPosition { get; set; }
        public int TotalCount { get; set; }
        public IReadOnlyCollection<Database.NicoVideo> Items { get; set; }

        public int Count => Items.Count;
    }
}
