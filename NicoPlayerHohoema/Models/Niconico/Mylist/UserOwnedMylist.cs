using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NicoPlayerHohoema.Models
{
    public class UserOwnedMylist : ReadOnlyObservableCollection<string>, Interfaces.IUserOwnedRemoteMylist
    {
        public const string DefailtMylistId = "0";

        public PlaylistOrigin Origin => PlaylistOrigin.LoginUser;
        public string Id => GroupId;
        public int SortIndex => 0;

		public string GroupId { get; private set; }
        public string UserId { get; set; }
		public string Label { get; set; }
		public string Description { get; set; }
		public bool IsPublic { get; set; }
		public IconType IconType { get; set; }
		public MylistDefaultSort Sort { get; set; }
        public int RegistrationLimit { get; set; }

        public List<Uri> ThumnailUrls { get; set; }

        public bool IsDeflist => GroupId == DefailtMylistId;

        public bool IsDefaultMylist => IsDeflist;

        public Provider.LoginUserMylistProvider LoginUserMylistProvider { get; }

        HashSet<string> _VideoIdHashSet = new HashSet<string>();


        public UserOwnedMylist(string groupId, IEnumerable<string> initialItems, Provider.LoginUserMylistProvider loginUserMylistProvider)
            : base(new ObservableCollection<string>(initialItems))
		{
			GroupId = groupId;
            LoginUserMylistProvider = loginUserMylistProvider;

            foreach (var item in Items)
            {
                _VideoIdHashSet.Add(item);
            }
        }

        private DelegateCommand<object> _AddItemCommand;
        public ICommand AddItemCommand => _AddItemCommand
            ?? (_AddItemCommand = new DelegateCommand<object>(async (p) =>
            {
                string videoId = null;
                if (p is Interfaces.IVideoContent videoItem)
                {
                    videoId = videoItem.Id;
                }
                else if (p is string maybeVideoId)
                {
                    var info = Database.NicoVideoDb.Get(maybeVideoId);
                    videoId = info?.RawVideoId;
                }

                if (videoId != null)
                {
                    var result = await AddMylistItem(videoId);

                    /*
                    var notificationService = Commands.HohoemaCommnadHelper.GetNotificationService();
                    notificationService.ShowInAppNotification(
                        Services.InAppNotificationPayload.CreateRegistrationResultNotification(
                            result,
                            "マイリスト",
                            Label,
                            videoItem.Label
                            ));
                            */
                }
            }
            , (p) => true
            ));


        private DelegateCommand<object> _RemoveItemCommand;
        public ICommand RemoveItemCommand => _RemoveItemCommand
            ?? (_RemoveItemCommand = new DelegateCommand<object>(async (p) =>
            {
                string videoId = null;
                if (p is Interfaces.IVideoContent videoItem)
                {
                    videoId = videoItem.Id;
                }
                else if (p is string maybeVideoId)
                {
                    var info = Database.NicoVideoDb.Get(maybeVideoId);
                    videoId = info?.RawVideoId;
                }

                if (videoId != null)
                {
                    var result = await RemoveMylistItem(videoId);

                    /*
                    var notificationService = Commands.HohoemaCommnadHelper.GetNotificationService();
                    notificationService.ShowInAppNotification(
                        Services.InAppNotificationPayload.CreateRegistrationResultNotification(
                            result,
                            "マイリスト",
                            Label,
                            videoItem.Label
                            ));
                            */
                }
            }
            , (p) => true
            ));



        public Windows.UI.Color ThemeColor
		{
			get
			{
				return IconType.ToColor();
			}
		}


		public int ItemCount
		{
			get
			{
                return Count;
			}
		}



        public async Task<bool> AddMylistItem(string videoId)
        {
            if (_VideoIdHashSet.Contains(videoId))
            {
                return false;
            }

            var result = await LoginUserMylistProvider.AddMylistItem(this.GroupId, videoId);
            if (result == ContentManageResult.Success)
            {
                Items.Add(videoId);
                _VideoIdHashSet.Add(videoId);
            }

            return result == ContentManageResult.Success;
        }

        public async Task<bool> RemoveMylistItem(string videoId)
        {
            if (!_VideoIdHashSet.Contains(videoId))
            {
                return false;
            }

            var result = await LoginUserMylistProvider.RemoveMylistItem(this.GroupId, videoId);
            if (result == ContentManageResult.Success)
            {
                Items.Remove(videoId);
                _VideoIdHashSet.Remove(videoId);
            }

            return result == ContentManageResult.Success;
        }



        public bool ContainsVideoId(string videoId)
		{
			return _VideoIdHashSet.Contains(videoId);
		}

        static private void SortMylistData(ref List<MylistData> mylist, MylistDefaultSort sort)
		{
			switch (sort)
			{
				case MylistDefaultSort.Old:
					mylist.Sort((x, y) => DateTime.Compare(x.UpdateTime, y.UpdateTime));
					break;
				case MylistDefaultSort.Latest:
					mylist.Sort((x, y) => -DateTime.Compare(x.UpdateTime, y.UpdateTime));
					break;
				case MylistDefaultSort.Memo_Ascending:
					mylist.Sort((x, y) => string.Compare(x.Description, y.Description));
					break;
				case MylistDefaultSort.Memo_Descending:
					mylist.Sort((x, y) => -string.Compare(x.Description, y.Description));
					break;
				case MylistDefaultSort.Title_Ascending:
					mylist.Sort((x, y) => string.Compare(x.Title, y.Title));
					break;
				case MylistDefaultSort.Title_Descending:
					mylist.Sort((x, y) => -string.Compare(x.Title, y.Title));
					break;
				case MylistDefaultSort.FirstRetrieve_Ascending:
					mylist.Sort((x, y) => DateTime.Compare(x.FirstRetrieve, y.FirstRetrieve));
					break;
				case MylistDefaultSort.FirstRetrieve_Descending:
					mylist.Sort((x, y) => - DateTime.Compare(x.FirstRetrieve, y.FirstRetrieve));
					break;
				case MylistDefaultSort.View_Ascending:
					mylist.Sort((x, y) => (int)(x.ViewCount - y.ViewCount));
					break;
				case MylistDefaultSort.View_Descending:
					mylist.Sort((x, y) => -(int)(x.ViewCount - y.ViewCount));
					break;
				case MylistDefaultSort.Comment_New:
					// Note: コメント順は非対応
					mylist.Sort((x, y) => (int)(x.CommentCount - y.CommentCount));
					break;
				case MylistDefaultSort.Comment_Old:
					// Note: コメント順は非対応
					mylist.Sort((x, y) => -(int)(x.CommentCount - y.CommentCount));
					break;
				case MylistDefaultSort.CommentCount_Ascending:
					mylist.Sort((x, y) => (int)(x.CommentCount - y.CommentCount));
					break;
				case MylistDefaultSort.CommentCount_Descending:
					mylist.Sort((x, y) => -(int)(x.CommentCount - y.CommentCount));
					break;
				case MylistDefaultSort.MylistCount_Ascending:
					mylist.Sort((x, y) => (int)(x.MylistCount - y.MylistCount));
					break;
				case MylistDefaultSort.MylistCount_Descending:
					mylist.Sort((x, y) => -(int)(x.MylistCount - y.MylistCount));
					break;
				case MylistDefaultSort.Length_Ascending:
					mylist.Sort((x, y) => TimeSpan.Compare(x.Length, y.Length));
					break;
				case MylistDefaultSort.Length_Descending:
					mylist.Sort((x, y) => -TimeSpan.Compare(x.Length, y.Length));
					break;
				default:
					break;
			}
		}

    }
}
