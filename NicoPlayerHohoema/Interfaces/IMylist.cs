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
    public interface IMylistItem : INiconicoContent, IFollowable
    {
    }

    public interface IMylist : IMylistItem, IReadOnlyCollection<string>
    {
        int SortIndex { get; }
        int ItemCount { get; }

        bool Contains(string videoId);
    }

    public interface IUserOwnedMylist : IMylist, ICollection<string>, INotifyCollectionChanged
    {
        Task<bool> AddMylistItem(string videoId);
        Task<bool> RemoveMylistItem(string videoId);

    }

    public interface IRemoteMylist : IMylist
    {
    }






    public interface IOtherOwnedMylist : IRemoteMylist, IFollowable
    {
        string UserId { get; }
    }

    public interface IUserOwnedRemoteMylist : IRemoteMylist, IUserOwnedMylist, IFollowable
    {
        bool IsDefaultMylist { get; }
    }

    public interface ILocalMylist : IUserOwnedMylist
    {
        new string Label { get; set; }
    }
}
