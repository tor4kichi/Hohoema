using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Ioc;
using NiconicoToolkit.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using NiconicoToolkit.Account;
using NiconicoToolkit.Video;
using System.Linq;
using Hohoema.Models.Domain.Playlist;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace Hohoema.Models.Domain.Niconico.Mylist.LoginUser
{
    public sealed class MylistItemRemovedEventArgs
    {
        public MylistId MylistId { get; internal set; }
        public IReadOnlyCollection<VideoId> SuccessedItems { get; internal set; }
        public IReadOnlyCollection<VideoId> FailedItems { get; internal set; }
    }


    public sealed class MylistItemAddedEventArgs
    {
        public MylistId MylistId { get; internal set; }
        public IReadOnlyCollection<VideoId> SuccessedItems { get; internal set; }
        public IReadOnlyCollection<VideoId> FailedItems { get; internal set; }
    }

    public sealed class MylistItemCopyEventArgs
    {
        public MylistId SourceMylistId { get; internal set; }
        public MylistId TargetMylistId { get; internal set; }
        public IReadOnlyCollection<VideoId> SuccessedItems { get; internal set; }
        public IReadOnlyCollection<VideoId> FailedItems { get; internal set; }
    }

    public sealed class MylistItemMovedEventArgs
    {
        public MylistId SourceMylistId { get; internal set; }
        public MylistId TargetMylistId { get; internal set; }
        public IReadOnlyCollection<VideoId> SuccessedItems { get; internal set; }
        public IReadOnlyCollection<VideoId> FailedItems { get; internal set; }
    }

    public class LoginUserMylistPlaylist : MylistPlaylist, IUserManagedPlaylist
    {
        LoginUserMylistProvider _loginUserMylistProvider;

        public int TotalCount => Count;

        public LoginUserMylistPlaylist(MylistId id, LoginUserMylistProvider loginUserMylistProvider)
            : base(id)
        {
            _loginUserMylistProvider = loginUserMylistProvider;
        }
        
        public async Task<bool> UpdateMylistInfo(string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            if (await _loginUserMylistProvider.UpdateMylist(this, name, description, isPublic, sortKey, sortOrder))
            {
                this.Name = name;
                this.Description = description;
                this.IsPublic = IsPublic;
                this.DefaultSortKey = sortKey;
                this.DefaultSortOrder = sortOrder;

                return true;
            }
            else
            {
                return false;
            }
        }

        public override async Task<MylistItemsGetResult> GetItemsAsync(int page, int pageSize, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            try
            {
                return await _loginUserMylistProvider.GetLoginUserMylistItemsAsync(this, page, pageSize, sortKey, sortOrder);
            }
            catch
            {

            }

            return new MylistItemsGetResult() { IsSuccess = false };
        }




        public Task<MylistItemAddedEventArgs> AddItem(VideoId videoId, string mylistComment = "")
        {
            return AddItem(new[] { videoId }, mylistComment);
        }

        public async Task<MylistItemAddedEventArgs> AddItem(IEnumerable<VideoId> items, string mylistComment = "")
        {
            List<VideoId> successed = new();
            List<VideoId> failed = new();

            foreach (var videoId in items)
            {
                var result = await _loginUserMylistProvider.AddMylistItem(MylistId, videoId, mylistComment);
                if (result != ContentManageResult.Failed)
                {
                    successed.Add(videoId);
                }
                else
                {
                    failed.Add(videoId);
                }
            }

            var args = new MylistItemAddedEventArgs()
            {
                MylistId = MylistId,
                SuccessedItems = successed,
                FailedItems = failed
            };
            MylistItemAdded?.Invoke(this, args);

            return args;
        }


        public Task<MylistItemRemovedEventArgs> RemoveItem(VideoId videoId)
        {
            return RemoveItem(new[] { videoId });
        }

        public async Task<MylistItemRemovedEventArgs> RemoveItem(IEnumerable<VideoId> items)
        {
            List<VideoId> successed = new();
            List<VideoId> failed = new();

            foreach (var videoId in items)
            {
                var result = await _loginUserMylistProvider.RemoveMylistItem(MylistId, videoId);
                if (result == ContentManageResult.Success)
                {
                    successed.Add(videoId);
                }
                else
                {
                    failed.Add(videoId);
                }
            }

            var args = new MylistItemRemovedEventArgs()
            {
                MylistId = MylistId,
                SuccessedItems = successed,
                FailedItems = failed
            };

            MylistItemRemoved?.Invoke(this, args);

            ClearAllWhenMylistChanged();

            return args;
        }


        public async Task<ContentManageResult> CopyItemAsync(MylistId targetMylistId, IEnumerable<VideoId> itemIds)
        {
            var result = await _loginUserMylistProvider.CopyMylistTo(MylistId, targetMylistId, itemIds);
            if (result.Meta.IsSuccess)
            {
                MylistCopied?.Invoke(this, new MylistItemCopyEventArgs()
                {
                    SourceMylistId = MylistId,
                    TargetMylistId = targetMylistId,
                    SuccessedItems = result.Data.ProcessedIds.Select(x => (VideoId)x).ToArray()
                });
            }

            return result.Meta.IsSuccess ? ContentManageResult.Success : ContentManageResult.Failed;
        }

        public async Task<ContentManageResult> MoveItemAsync(MylistId targetMylistId, IEnumerable<VideoId> itemIds)
        {
            var result = await _loginUserMylistProvider.MoveMylistTo(MylistId, targetMylistId, itemIds);
            if (result.Meta.IsSuccess)
            {
                MylistMoved?.Invoke(this, new MylistItemMovedEventArgs()
                {
                    SourceMylistId = MylistId,
                    TargetMylistId = targetMylistId,
                    SuccessedItems = result.Data.ProcessedIds.Select(x => (VideoId)x).ToArray()
                });

                ClearAllWhenMylistChanged();
            }

            return result.Meta.IsSuccess ? ContentManageResult.Success : ContentManageResult.Failed;
        }


        public event EventHandler<MylistItemAddedEventArgs> MylistItemAdded;
        public event EventHandler<MylistItemRemovedEventArgs> MylistItemRemoved;

        public event EventHandler<MylistItemCopyEventArgs> MylistCopied;
        public event EventHandler<MylistItemMovedEventArgs> MylistMoved;

        // IShufflePlaylistItemsSource 
        private void ClearAllWhenMylistChanged()
        {
            foreach (var i in Enumerable.Range(0, Count))
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, null, i));
            }

        }
        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
