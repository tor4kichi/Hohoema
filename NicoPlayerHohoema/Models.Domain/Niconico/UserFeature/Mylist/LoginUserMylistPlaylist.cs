using Mntone.Nico2;
using Hohoema.Models.UseCase.Playlist.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Ioc;
using Mntone.Nico2.Users.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Presentation.Services;

namespace Hohoema.Models.Domain.Niconico.UserFeature.Mylist
{
    public class LoginUserMylistPlaylist : MylistPlaylist
    {
        LoginUserMylistProvider _loginUserMylistProvider;

        public LoginUserMylistPlaylist(string id, LoginUserMylistProvider loginUserMylistProvider)
            : base(id)
        {
            _loginUserMylistProvider = loginUserMylistProvider;
            ItemsRemoveCommand = new MylistRemoveItemCommand(this);
            ItemsAddCommand = new MylistAddItemCommand(this, App.Current.Container.Resolve<NotificationService>());
        }

        public MylistRemoveItemCommand ItemsRemoveCommand { get; }
        public MylistAddItemCommand ItemsAddCommand { get; }

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

        public Task<List<IVideoContent>> GetLoginUserMylistItemsAsync(MylistSortKey sortKey, MylistSortOrder sortOrder, uint pageSize, uint page)
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

        public event EventHandler<MylistItemAddedEventArgs> MylistItemAdded;
        public event EventHandler<MylistItemRemovedEventArgs> MylistItemRemoved;


    }
}
