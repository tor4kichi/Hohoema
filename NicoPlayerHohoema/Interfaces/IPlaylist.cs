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
        Mntone.Nico2.Mylist.IconType IconType { get; }
        Mntone.Nico2.Order Order { get; }
        Mntone.Nico2.Sort Sort { get; }
        DateTime UpdateTime { get; }
        DateTime CreateTime { get; }
    }

    public interface IPlaylist : INiconicoContent
    {
        int SortIndex { get; }
        int Count { get; }
    }
}
