using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NiconicoToolkit.Mylist;
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Account;
using NiconicoToolkit.Video;
using System.Linq;
using Hohoema.Models.Playlist;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace Hohoema.Models.Niconico.Mylist.LoginUser
{
    public sealed class MylistItemRemovedEventArgs
    {
        public MylistId MylistId { get; internal set; }
        public IReadOnlyCollection<IVideoContent> SuccessedItems { get; internal set; }
        public IReadOnlyCollection<IVideoContent> FailedItems { get; internal set; }
    }


    public sealed class MylistItemAddedEventArgs
    {
        public MylistId MylistId { get; internal set; }
        public IReadOnlyCollection<IVideoContent> SuccessedItems { get; internal set; }
        public IReadOnlyCollection<IVideoContent> FailedItems { get; internal set; }
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




        public Task<MylistItemAddedEventArgs> AddItem(IVideoContent video, string mylistComment = "")
        {
            return AddItem(new[] { video }, mylistComment);
        }

        public async Task<MylistItemAddedEventArgs> AddItem(IEnumerable<IVideoContent> items, string mylistComment = "")
        {
            List<IVideoContent> successed = new();
            List<IVideoContent> failed = new();

            foreach (var video in items)
            {
                var result = await _loginUserMylistProvider.AddMylistItem(MylistId, video.VideoId, mylistComment);
                if (result != ContentManageResult.Failed)
                {
                    successed.Add(video);
                }
                else
                {
                    failed.Add(video);
                }
            }

            var args = new MylistItemAddedEventArgs()
            {
                MylistId = MylistId,
                SuccessedItems = successed,
                FailedItems = failed
            };
            
            MylistItemAdded?.Invoke(this, args);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, successed));

            return args;
        }


        public Task<MylistItemRemovedEventArgs> RemoveItem(PlaylistItemToken itemToken)
        {
            return RemoveItem(new[] { itemToken });
        }

        public async Task<MylistItemRemovedEventArgs> RemoveItem(IEnumerable<PlaylistItemToken> items)
        {
            List<IVideoContent> successed = new();
            List<IVideoContent> failed = new();

            foreach (var item in items)
            {
                var (_, _, video) = item;
                var result = await _loginUserMylistProvider.RemoveMylistItem(MylistId, video.VideoId);
                if (result == ContentManageResult.Success)
                {
                    successed.Add(video);
                }
                else
                {
                    failed.Add(video);
                }
            }

            var args = new MylistItemRemovedEventArgs()
            {
                MylistId = MylistId,
                SuccessedItems = successed,
                FailedItems = failed
            };

            MylistItemRemoved?.Invoke(this, args);

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, successed));

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

                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            return result.Meta.IsSuccess ? ContentManageResult.Success : ContentManageResult.Failed;
        }


        public event EventHandler<MylistItemAddedEventArgs> MylistItemAdded;
        public event EventHandler<MylistItemRemovedEventArgs> MylistItemRemoved;

        public event EventHandler<MylistItemCopyEventArgs> MylistCopied;
        public event EventHandler<MylistItemMovedEventArgs> MylistMoved;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
