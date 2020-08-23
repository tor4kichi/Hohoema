using Mntone.Nico2.Users.Mylist;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NicoPlayerHohoema.Interfaces
{
    public interface IMylist : IPlaylist, IFollowable
    {
        string Description { get; }
        string UserId { get; }
        bool IsPublic { get; }
        MylistSortOrder DefaultSortOrder { get; }
        MylistSortKey DefaultSortKey { get; }
        DateTime CreateTime { get; }
    }

    public interface IPlaylist : INiconicoContent
    {
        int SortIndex { get; }
        int Count { get; }
    }
}
