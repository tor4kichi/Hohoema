using NicoPlayerHohoema.Models;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Prism.Mvvm;
using Prism.Commands;
using Mntone.Nico2.Mylist.MylistGroup;
using System.Collections.ObjectModel;
using Reactive.Bindings;
using Mntone.Nico2.Mylist;
using Reactive.Bindings.Extensions;
using System.Threading;
using NicoPlayerHohoema.Views.Service;
using Microsoft.Practices.Unity;
using System.Reactive.Linq;
using Windows.UI;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using NicoPlayerHohoema.Dialogs;
using Mntone.Nico2.Searches.Mylist;
using NicoPlayerHohoema.Helpers;
using System.Collections.Async;

namespace NicoPlayerHohoema.ViewModels
{
	public class UserMylistPageViewModel : HohoemaListingPageViewModelBase<IPlayableList>
	{
        public UserMylistManager UserMylistManager { get; private set; }
        public OtherOwneredMylistManager OtherOwneredMylistManager { get; }


        public string UserId { get; private set; }

        private string _UserName;
        public string UserName
        {
            get { return _UserName; }
            set { SetProperty(ref _UserName, value); }
        }


        public ReactiveProperty<bool> IsLoginUserMylist { get; private set; }


        public ReactiveCommand<IPlayableList> OpenMylistCommand { get; private set; }
        public DelegateCommand AddMylistGroupCommand { get; private set; }
        public DelegateCommand<IPlayableList> RemoveMylistGroupCommand { get; private set; }
        public DelegateCommand<IPlayableList> EditMylistGroupCommand { get; private set; }
        public DelegateCommand<IPlayableList> PlayAllCommand { get; private set; }

        public DelegateCommand AddLocalMylistCommand { get; private set; }

        public UserMylistPageViewModel(HohoemaApp app, PageManager pageMaanger)
			: base(app, pageMaanger, useDefaultPageTitle: false)
		{
            OtherOwneredMylistManager = app.OtherOwneredMylistManager;
            UserMylistManager = app.UserMylistManager;

            IsLoginUserMylist = new ReactiveProperty<bool>(false);

            OpenMylistCommand = new ReactiveCommand<IPlayableList>();

			OpenMylistCommand.Subscribe(listItem => 
			{
				PageManager.OpenPage(HohoemaPageType.Mylist, 
                    new MylistPagePayload(listItem).ToParameterString()
                    );
			});

			AddMylistGroupCommand = new DelegateCommand(async () => 
			{
				MylistGroupEditData data = new MylistGroupEditData()
				{
					Name = "新しいマイリスト",
					Description = "",
					IsPublic = false,
					MylistDefaultSort = MylistDefaultSort.Latest,
					IconType = IconType.Default,
				};

				var dialogService = App.Current.Container.Resolve<Services.HohoemaDialogService>();

				// 成功するかキャンセルが押されるまで繰り返す
				while (true)
				{
					if (true == await dialogService.ShowCreateMylistGroupDialogAsync(data))
					{
						var result = await HohoemaApp.UserMylistManager.AddMylist(
							data.Name,
							data.Description,
							data.IsPublic,
							data.MylistDefaultSort,
							data.IconType
						);

						if (result == Mntone.Nico2.ContentManageResult.Success)
						{
							await ResetList();
							break;
						}
					}
					else
					{
						break;
					}
				}


			}
			, () => UserMylistManager.UserMylists.Count < UserMylistManager.MaxMylistGroupCountCurrentUser
            );

            RemoveMylistGroupCommand = new DelegateCommand<IPlayableList>(async (item) => 
            {
                if (item.Origin == PlaylistOrigin.Local)
                {
                    if (item.Id == HohoemaPlaylist.WatchAfterPlaylistId) { return; }
                }
                else if (item.Origin == PlaylistOrigin.LoginUser)
                {
                    if (item.Id == "0") { return; }
                }

                // 確認ダイアログ
                var originText = item.Origin == PlaylistOrigin.Local ? "ローカルマイリスト" : "マイリスト";
                var contentMessage = $"{item.Label} を削除してもよろしいですか？（変更は元に戻せません）";

                var dialog = new MessageDialog(contentMessage, $"{originText}削除の確認");
                dialog.Commands.Add(new UICommand("削除", async (i) =>
                {
                    if (item.Origin == PlaylistOrigin.Local)
                    {
                        await HohoemaApp.Playlist.RemovePlaylist(item as LocalMylist);
                    }
                    else if (item.Origin == PlaylistOrigin.LoginUser)
                    {
                        await HohoemaApp.UserMylistManager.RemoveMylist(item.Id);
//                        await UpdateUserMylist();
                    }
                }));

                dialog.Commands.Add(new UICommand("キャンセル"));
                dialog.CancelCommandIndex = 1;
                dialog.DefaultCommandIndex = 1;

                await dialog.ShowAsync();
            });


			EditMylistGroupCommand = new DelegateCommand<IPlayableList>(async item => 
			{
                if (item.Origin == PlaylistOrigin.Local)
                {
                    if (item.Id == HohoemaPlaylist.WatchAfterPlaylistId) { return; }
                }
                else if (item.Origin == PlaylistOrigin.LoginUser)
                {
                    if (item.Id == "0") { return; }
                }

                if (item.Origin == PlaylistOrigin.Local)
                {
                    var dialogService = App.Current.Container.Resolve<Services.HohoemaDialogService>();
                    var localMylist = item as LocalMylist;
                    var resultText = await dialogService.GetTextAsync("プレイリスト名を変更",
                        localMylist.Label,
                        localMylist.Label,
                        (tempName) => !string.IsNullOrWhiteSpace(tempName)
                        );

                    if (!string.IsNullOrWhiteSpace(resultText))
                    {
                        localMylist.Label = resultText;
                    }
                }


                if (item.Origin == PlaylistOrigin.LoginUser)
                {
                    var mylistGroupListItem = item as MylistGroupInfo;
                    var selectedMylistGroupId = mylistGroupListItem.Id;

                    if (selectedMylistGroupId == null) { return; }

                    var mylistGroup = HohoemaApp.UserMylistManager.GetMylistGroup(selectedMylistGroupId);
                    MylistGroupEditData data = new MylistGroupEditData()
                    {
                        Name = mylistGroup.Label,
                        Description = mylistGroup.Description,
                        IsPublic = mylistGroup.IsPublic,
                        MylistDefaultSort = mylistGroup.Sort,
                        IconType = mylistGroup.IconType,
                    };

                    // 成功するかキャンセルが押されるまで繰り返す
                    while (true)
                    {
                        var dialogService = App.Current.Container.Resolve<Services.HohoemaDialogService>();
                        if (true == await dialogService.ShowCreateMylistGroupDialogAsync(data))
                        {
                            var result = await mylistGroup.UpdateMylist(
                                data.Name,
                                data.Description,
                                data.IsPublic,
                                data.MylistDefaultSort,
                                data.IconType
                            );

                            if (result == Mntone.Nico2.ContentManageResult.Success)
                            {
                                // TODO: UI上のマイリスト表示を更新する
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                

            });

            PlayAllCommand = new DelegateCommand<IPlayableList>((item) => 
            {
                if (item.PlaylistItems.Count == 0) { return; }

                HohoemaApp.Playlist.Play(item.PlaylistItems.First());
            });



            AddLocalMylistCommand = new DelegateCommand(async () => 
            {
                var dialogService = App.Current.Container.Resolve<Services.HohoemaDialogService>();
                var name = await dialogService.GetTextAsync("新しいローカルマイリスト名を入力", "ローカルマイリスト名", "",
                    (s) => 
                    {
                        if (string.IsNullOrWhiteSpace(s)) { return false; }

                        if (HohoemaApp.Playlist.Playlists.Any(x => x.Label == s))
                        {
                            return false;
                        }

                        return true;
                    });

                if (name != null)
                {
                    var newLocalMylist = HohoemaApp.Playlist.CreatePlaylist(Guid.NewGuid().ToString(), name);
                }
            });

        }


        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);
		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				UserId = e.Parameter as string;				
			}

            if (UserId == null || UserId == HohoemaApp.LoginUserId.ToString())
            {
                UpdateTitle(PageManager.CurrentDefaultPageTitle());

                IsLoginUserMylist.Value = true;

                // ログインユーザーのマイリスト一覧を表示
                UserName = HohoemaApp.LoginUserName;

                await HohoemaApp.UserMylistManager.Initialize();
            }
            else if (UserId != null)
			{
                try
				{
					var userInfo = await HohoemaApp.ContentProvider.GetUser(UserId);
					UserName = userInfo.ScreenName;
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);
				}

                UpdateTitle($"{UserName} さんのマイリスト一覧");
            }
            else
            {
                throw new Exception("UserMylistPage が不明なパラメータと共に開かれました : " + e.Parameter);
            } 
			
			AddMylistGroupCommand.RaiseCanExecuteChanged();

            await base.NavigatedToAsync(cancelToken, e, viewModelState);
		}


        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            UserId = null;
            IsLoginUserMylist.Value = false;
            UserName = "";

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        protected override IIncrementalSource<IPlayableList> GenerateIncrementalSource()
        {
            if (!IsLoginUserMylist.Value && UserId != null)
            {
                return new OtherUserMylistIncrementalLoadingSource(UserId, HohoemaApp.OtherOwneredMylistManager);
            }
            else
            {
                if (HohoemaApp.IsLoggedIn)
                {
                    var items =
                        Enumerable.Concat(
                            HohoemaApp.Playlist.Playlists.Cast<IPlayableList>(),
                            HohoemaApp.UserMylistManager?.UserMylists ?? Enumerable.Empty<IPlayableList>()
                        )
                        .ToList();
                    return new ImmidiateIncrementalLoadingCollectionSource<IPlayableList>(items);
                }
                else
                {
                    var items = 
                        HohoemaApp.Playlist.Playlists.Cast<IPlayableList>()
                        .ToList();

                    return new ImmidiateIncrementalLoadingCollectionSource<IPlayableList>(items);

                }
            }
        }
    }

    public sealed class OtherUserMylistIncrementalLoadingSource : HohoemaIncrementalSourceBase<IPlayableList>
    {
        List<OtherOwneredMylist> OtherUserMylists { get; set; }

        public string UserId { get; }
        public OtherOwneredMylistManager OtherOwneredMylistManager;
        public OtherUserMylistIncrementalLoadingSource(string userId, OtherOwneredMylistManager otherOwneredMylistManager)
        {
            UserId = userId;
            OtherOwneredMylistManager = otherOwneredMylistManager;
        }

        protected override Task<IAsyncEnumerable<IPlayableList>> GetPagedItemsImpl(int head, int count)
        {
            return Task.FromResult(OtherUserMylists.Skip(head).Take(count).Cast<IPlayableList>().ToAsyncEnumerable());
        }

        protected override async Task<int> ResetSourceImpl()
        {
            try
            {
                OtherUserMylists = await OtherOwneredMylistManager.GetByUserId(UserId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return OtherUserMylists?.Count ?? 0;
        }
    }



    public class MylistGroupListItem : HohoemaListingPageItemBase, Interfaces.IMylist
	{
        public MylistGroupListItem(IPlayableList list, PageManager pageManager)
        {
            _PageManager = pageManager;

            GroupId = list.Id;

            Label = list.Label;

            if (list.ThumnailUrl != null)
            {
                AddImageUrl(list.ThumnailUrl);
            }
        }


        public MylistGroupListItem(MylistGroupInfo info, PageManager pageManager)
		{
			_PageManager = pageManager;

			GroupId = info.GroupId;

			Update(info);

            if (info.ThumnailUrl != null)
            {
                AddImageUrl(info.ThumnailUrl);
            }
        }

		public MylistGroupListItem(MylistGroupData mylistGroup, PageManager pageManager)
		{
			_PageManager = pageManager;

			Label = mylistGroup.Name;
			Description = mylistGroup.Description;
			GroupId = mylistGroup.Id;
            OptionText = (mylistGroup.GetIsPublic() ? "公開" : "非公開") + $" - {mylistGroup.Count}件";

            ThemeColor = mylistGroup.GetIconType().ToColor();
            ItemCount = (uint)mylistGroup.Count;

            if (mylistGroup.ThumbnailUrls != null)
            {
                foreach (var thumbnailUri in mylistGroup.ThumbnailUrls)
                {
                    AddImageUrl(thumbnailUri.OriginalString);
                }
            }
        }

        public MylistGroupListItem(MylistGroup mylistGroup, PageManager pageManager)
        {
            _PageManager = pageManager;

            Label = mylistGroup.Name;
            Description = mylistGroup.Description;
            GroupId = mylistGroup.Id;
            OptionText = ("公開") + $" - {mylistGroup.ItemCount}件";
            ItemCount = mylistGroup.ItemCount;

            foreach (var thumbnailUri in mylistGroup.VideoInfoItems.Take(3).Select(x => x.Video.ThumbnailUrl))
            {
                AddImageUrl(thumbnailUri.OriginalString);
            }
        }

        public MylistGroupListItem(Mylistgroup mylistGroup, PageManager pageManager)
        {
            _PageManager = pageManager;

            Label = mylistGroup.Name;
            Description = mylistGroup.Description;
            GroupId = mylistGroup.Id;
            OptionText = ("公開") + $" - {mylistGroup.Count}件";
            ItemCount = mylistGroup.Count;

            foreach (var thumbnailUri in mylistGroup.SampleVideoInfoItems.Select(x => x.Video.ThumbnailUrl))
            {
                AddImageUrl(thumbnailUri.OriginalString);
            }
        }

        public void Update(MylistGroupInfo info)
		{
			Label = info.Label;
			Description = info.Description;
			OptionText = (info.IsPublic ? "公開" : "非公開") + $" - {info.PlaylistItems.Count}件";

            ThemeColor = info.IconType.ToColor();

            // ユーザーマイリストの情報はそのままではサムネが取れない
            // マイリスト内の動画からサムネを取得する？
        }

		public string GroupId { get; private set; }

        public string Id => GroupId;

        public uint ItemCount { get; private set; }
        public DateTime UpdateTime { get; private set; }
        public List<Mntone.Nico2.Searches.Video.Video> SampleVideos { get; private set; }


        PageManager _PageManager;
	}

    public class MylistItemsWithTitle : ListWithTitle<MylistSearchListingItem>
    {
        public PlaylistOrigin Origin { get; set; }
        public string MaxItemsCountText { get; set; }
    }


    public class ListWithTitle<T>
    {
        public string Title { get; set; }
        public IList<T> Items { get; set; }
    }
}
