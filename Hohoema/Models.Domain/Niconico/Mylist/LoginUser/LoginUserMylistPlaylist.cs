using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Ioc;
using Mntone.Nico2.Users.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;

namespace Hohoema.Models.Domain.Niconico.Mylist.LoginUser
{
    public sealed class MylistItemRemovedEventArgs
    {
        public string MylistId { get; internal set; }
        public IReadOnlyCollection<string> SuccessedItems { get; internal set; }
        public IReadOnlyCollection<string> FailedItems { get; internal set; }
    }


    public sealed class MylistItemAddedEventArgs
    {
        public string MylistId { get; internal set; }
        public IReadOnlyCollection<string> SuccessedItems { get; internal set; }
        public IReadOnlyCollection<string> FailedItems { get; internal set; }
    }

    public sealed class MylistItemCopyEventArgs
    {
        public string SourceMylistId { get; internal set; }
        public string TargetMylistId { get; internal set; }
        public IReadOnlyCollection<string> SuccessedItems { get; internal set; }
        public IReadOnlyCollection<string> FailedItems { get; internal set; }
    }

    public sealed class MylistItemMovedEventArgs
    {
        public string SourceMylistId { get; internal set; }
        public string TargetMylistId { get; internal set; }
        public IReadOnlyCollection<string> SuccessedItems { get; internal set; }
        public IReadOnlyCollection<string> FailedItems { get; internal set; }
    }

    public class LoginUserMylistPlaylist : MylistPlaylist
    {
        LoginUserMylistProvider _loginUserMylistProvider;

        public LoginUserMylistPlaylist(string id, LoginUserMylistProvider loginUserMylistProvider)
            : base(id)
        {
            _loginUserMylistProvider = loginUserMylistProvider;
        }
        
        public async Task<bool> UpdateMylistInfo(string mylistId, string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            if (await _loginUserMylistProvider.UpdateMylist(mylistId, name, description, isPublic, sortKey, sortOrder))
            {
                this.Label = name;
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

        public async Task<List<IVideoContent>> GetAll(MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            List<IVideoContent> items = new List<IVideoContent>();
            uint page = 0;

            while (items.Count != Count)
            {
                var res = await _loginUserMylistProvider.GetLoginUserMylistItemsAsync(this, sortKey, sortOrder, 25, page);
                items.AddRange(res);
                page++;
            }

            return items;
        }

        public Task<List<NicoVideo>> GetLoginUserMylistItemsAsync(MylistSortKey sortKey, MylistSortOrder sortOrder, uint pageSize, uint page)
        {
            return _loginUserMylistProvider.GetLoginUserMylistItemsAsync(this, sortKey, sortOrder, pageSize, page);
        }




        public Task<MylistItemAddedEventArgs> AddItem(string videoId, string mylistComment = "")
        {
            return AddItem(new[] { videoId }, mylistComment);
        }

        public async Task<MylistItemAddedEventArgs> AddItem(IEnumerable<string> items, string mylistComment = "")
        {
            List<string> successed = new List<string>();
            List<string> failed = new List<string>();

            foreach (var videoId in items)
            {
                var result = await _loginUserMylistProvider.AddMylistItem(Id, videoId, mylistComment);
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
                MylistId = Id,
                SuccessedItems = successed,
                FailedItems = failed
            };
            MylistItemAdded?.Invoke(this, args);

            return args;
        }


        public Task<MylistItemRemovedEventArgs> RemoveItem(string videoId)
        {
            return RemoveItem(new[] { videoId });
        }

        public async Task<MylistItemRemovedEventArgs> RemoveItem(IEnumerable<string> items)
        {
            List<string> successed = new List<string>();
            List<string> failed = new List<string>();

            foreach (var videoId in items)
            {
                var result = await _loginUserMylistProvider.RemoveMylistItem(Id, videoId);
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
                MylistId = Id,
                SuccessedItems = successed,
                FailedItems = failed
            };
            MylistItemRemoved?.Invoke(this, args);

            return args;
        }

        public async Task<ContentManageResult> CopyItemAsync(string targetMylistId, params string[] itemIds)
        {
            var result = await _loginUserMylistProvider.CopyMylistTo(this.Id, targetMylistId, itemIds);
            if (result == ContentManageResult.Success)
            {
                MylistCopied?.Invoke(this, new MylistItemCopyEventArgs()
                {
                    SourceMylistId = this.Id,
                    TargetMylistId = targetMylistId,
                    SuccessedItems = itemIds
                });
            }

            return result;
        }

        public async Task<ContentManageResult> MoveItemAsync(string targetMylistId, params string[] itemIds)
        {
            var result = await _loginUserMylistProvider.MoveMylistTo(this.Id, targetMylistId, itemIds);
            if (result == ContentManageResult.Success)
            {
                MylistMoved?.Invoke(this, new MylistItemMovedEventArgs()
                {
                    SourceMylistId = this.Id,
                    TargetMylistId = targetMylistId,
                    SuccessedItems = itemIds
                });
            }

            return result;
        }

        public event EventHandler<MylistItemAddedEventArgs> MylistItemAdded;
        public event EventHandler<MylistItemRemovedEventArgs> MylistItemRemoved;

        public event EventHandler<MylistItemCopyEventArgs> MylistCopied;
        public event EventHandler<MylistItemMovedEventArgs> MylistMoved;

    }
}
