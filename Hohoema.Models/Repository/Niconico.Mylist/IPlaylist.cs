using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.Models.Repository.Niconico;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hohoema.Models.Repository.Niconico.Mylist
{
    public interface IMylist : IPlaylist, IFollowable
    {
        string Description { get; }
        string UserId { get; }
        bool IsPublic { get; }
        MylistGroupIconType IconType { get; }
        Order Order { get; }
        Sort Sort { get; }
        DateTime UpdateTime { get; }
        DateTime CreateTime { get; }
    }

    public interface IPlaylist : INiconicoContent
    {
        int SortIndex { get; }
        int Count { get; }
    }
}
