using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Ioc;
using NiconicoToolkit.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using NiconicoToolkit.Account;
using NiconicoToolkit.Video;

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

    public class LoginUserMylistPlaylist : MylistPlaylist
    {
        LoginUserMylistProvider _loginUserMylistProvider;

        public LoginUserMylistPlaylist(MylistId id, LoginUserMylistProvider loginUserMylistProvider)
            : base(id)
        {
            _loginUserMylistProvider = loginUserMylistProvider;
        }
        
        public async Task<bool> UpdateMylistInfo(MylistId mylistId, string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            if (await _loginUserMylistProvider.UpdateMylist(mylistId, name, description, isPublic, sortKey, sortOrder))
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

        public async Task<List<(MylistItem MylistItem, NicoVideo NicoVideo)>> GetAll(MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            List<(MylistItem, NicoVideo)> items = new();
            int page = 0;

            while (items.Count != Count)
            {
                var res = await _loginUserMylistProvider.GetLoginUserMylistItemsAsync(this, page, 25, sortKey, sortOrder);
                items.AddRange(res);
                page++;
            }

            return items;
        }

        public Task<List<(MylistItem MylistItem, NicoVideo NicoVideo)>> GetLoginUserMylistItemsAsync(int page, int pageSize, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            return _loginUserMylistProvider.GetLoginUserMylistItemsAsync(this, page, pageSize, sortKey, sortOrder);
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

            return args;
        }

        public async Task<ContentManageResult> CopyItemAsync(MylistId targetMylistId, params VideoId[] itemIds)
        {
            var result = await _loginUserMylistProvider.CopyMylistTo(MylistId, targetMylistId, itemIds);
            if (result.Meta.IsSuccess)
            {
                MylistCopied?.Invoke(this, new MylistItemCopyEventArgs()
                {
                    SourceMylistId = MylistId,
                    TargetMylistId = targetMylistId,
                    SuccessedItems = itemIds
                });
            }

            return result.Meta.IsSuccess ? ContentManageResult.Success : ContentManageResult.Failed;
        }

        public async Task<ContentManageResult> MoveItemAsync(MylistId targetMylistId, params VideoId[] itemIds)
        {
            var result = await _loginUserMylistProvider.MoveMylistTo(MylistId, targetMylistId, itemIds);
            if (result.Meta.IsSuccess)
            {
                MylistMoved?.Invoke(this, new MylistItemMovedEventArgs()
                {
                    SourceMylistId = MylistId,
                    TargetMylistId = targetMylistId,
                    SuccessedItems = itemIds
                });
            }

            return result.Meta.IsSuccess ? ContentManageResult.Success : ContentManageResult.Failed;
        }

        public event EventHandler<MylistItemAddedEventArgs> MylistItemAdded;
        public event EventHandler<MylistItemRemovedEventArgs> MylistItemRemoved;

        public event EventHandler<MylistItemCopyEventArgs> MylistCopied;
        public event EventHandler<MylistItemMovedEventArgs> MylistMoved;

    }
}
