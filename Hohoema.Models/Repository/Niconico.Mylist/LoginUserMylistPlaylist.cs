using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Mylist
{
    public class LoginUserMylistPlaylist : MylistPlaylist
    {
        LoginUserMylistProvider _loginUserMylistProvider;

        public LoginUserMylistPlaylist(string id, LoginUserMylistProvider loginUserMylistProvider)
            : base(id)
        {
            _loginUserMylistProvider = loginUserMylistProvider;
        }

        public MylistGroupDefaultSort DefaultSort { get; internal set; }


        public Task<List<Database.NicoVideo>> GetLoginUserMylistItemsAsync()
        {
            return _loginUserMylistProvider.GetLoginUserMylistItemsAsync(this);
        }


        public Task<ContentManageResult> UpdateMylist(MylistGroupEditData editData)
        {
            return _loginUserMylistProvider.UpdateMylist(Id, editData);
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
