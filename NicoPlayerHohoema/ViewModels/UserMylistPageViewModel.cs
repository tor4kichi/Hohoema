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

namespace NicoPlayerHohoema.ViewModels
{
	public class UserMylistPageViewModel : HohoemaViewModelBase
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


        public ObservableCollection<MylistItemsWithTitle> MylistList { get; }
        public ReactiveProperty<bool> IsLoginUserMylist { get; private set; }


        public ReactiveCommand<IPlayableList> OpenMylistCommand { get; private set; }
        public DelegateCommand AddMylistGroupCommand { get; private set; }
        public DelegateCommand<IPlayableList> RemoveMylistGroupCommand { get; private set; }
        public DelegateCommand<IPlayableList> EditMylistGroupCommand { get; private set; }
        public DelegateCommand<IPlayableList> PlayAllCommand { get; private set; }

        public DelegateCommand AddLocalMylistCommand { get; private set; }



        public UserMylistPageViewModel(HohoemaApp app, PageManager pageMaanger)
			: base(app, pageMaanger)
		{
            OtherOwneredMylistManager = app.OtherOwneredMylistManager;

            MylistList = new ObservableCollection<MylistItemsWithTitle>();

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

				var editDialog = App.Current.Container.Resolve<EditMylistGroupDialogService>();

				// 成功するかキャンセルが押されるまで繰り返す
				while (true)
				{
					if (true == await editDialog.ShowAsyncWithCreateMode(data))
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
							await UpdateUserMylist();
							break;
						}
					}
					else
					{
						break;
					}
				}


			}
			, () => HohoemaApp.UserMylistManager.UserMylists.Count < UserMylistManager.MaxUserMylistGroupCount
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
                var contentMessage = $"{item.Name} を削除してもよろしいですか？（変更は元に戻せません）";

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

                        await UpdateUserMylist();
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
                    var textInputDialogService = App.Current.Container.Resolve<Views.Service.TextInputDialogService>();
                    var localMylist = item as LocalMylist;
                    var resultText = await textInputDialogService.GetTextAsync("プレイリスト名を変更",
                        localMylist.Name,
                        localMylist.Name,
                        (tempName) => !string.IsNullOrWhiteSpace(tempName)
                        );

                    if (!string.IsNullOrWhiteSpace(resultText))
                    {
                        localMylist.Name = resultText;
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
                        Name = mylistGroup.Name,
                        Description = mylistGroup.Description,
                        IsPublic = mylistGroup.IsPublic,
                        MylistDefaultSort = mylistGroup.Sort,
                        IconType = mylistGroup.IconType,
                    };

                    var editDialog = App.Current.Container.Resolve<EditMylistGroupDialogService>();

                    // 成功するかキャンセルが押されるまで繰り返す
                    while (true)
                    {
                        if (true == await editDialog.ShowAsync(data))
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



            AddLocalMylistCommand = new DelegateCommand(() => 
            {
                var newLocalMylist = HohoemaApp.Playlist.CreatePlaylist(new Guid().ToString(), "新しいプレイリスト");
                OpenMylistCommand.Execute(newLocalMylist);
            });

        }


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);
		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            List<IPlayableList> mylists = null;

			if (e.Parameter is string)
			{
				UserId = e.Parameter as string;				
			}

            MylistList.Clear();

            IsLoginUserMylist.Value = UserId == null || UserId == HohoemaApp.LoginUserId.ToString();


            if (!IsLoginUserMylist.Value && UserId != null)
			{
                try
				{
					var userInfo = await HohoemaApp.ContentFinder.GetUserInfo(UserId);
					UserName = userInfo.Nickname;
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);
				}

				try
				{
					var items = await OtherOwneredMylistManager.GetByUserId(UserId);
                    mylists = items.Cast<IPlayableList>().ToList();
                }
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);
				}

                MylistList.Add(new MylistItemsWithTitle()
                {
                    Title = "マイリスト",
                    Origin = PlaylistOrigin.OtherUser,
                    Items = mylists.ToList(),
                });

                OnPropertyChanged(nameof(MylistList));
			}
			else if (IsLoginUserMylist.Value)
			{
				// ログインユーザーのマイリスト一覧を表示
				UserName = HohoemaApp.LoginUserName;

				await UpdateUserMylist();
			}
            else
            {
                throw new Exception("UserMylistPage が不明なパラメータと共に開かれました : " + e.Parameter);
            } 

			
			UpdateTitle($"{UserName} さんのマイリスト一覧");

			AddMylistGroupCommand.RaiseCanExecuteChanged();
		}


        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            MylistList.Clear();

            UserId = null;
            IsLoginUserMylist.Value = false;
            UserName = "";

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        private async Task UpdateUserMylist()
		{
			if (!HohoemaApp.MylistManagerUpdater.IsOneOrMoreUpdateCompleted)
			{
				HohoemaApp.MylistManagerUpdater.ScheduleUpdate();
				await HohoemaApp.MylistManagerUpdater.WaitUpdate();
			}

            var listItems = HohoemaApp.UserMylistManager.UserMylists;

            MylistList.Add(new MylistItemsWithTitle()
            {
                Title = "マイリスト",
                Origin = PlaylistOrigin.LoginUser,
                Items = listItems.ToReadOnlyReactiveCollection(x => x as IPlayableList)
            });
            MylistList.Add(new MylistItemsWithTitle()
            {
                Title = "ローカル",
                Origin = PlaylistOrigin.Local,
                Items = HohoemaApp.Playlist.Playlists.ToReadOnlyReactiveCollection(x => x as IPlayableList)
            });
        }
	}



	public class MylistGroupListItem : HohoemaListingPageItemBase
	{
        public MylistGroupListItem(IPlayableList list, PageManager pageManager)
        {
            _PageManager = pageManager;

            GroupId = list.Id;

            Title = list.Name;
            
        }


        public MylistGroupListItem(MylistGroupInfo info, PageManager pageManager)
		{
			_PageManager = pageManager;

			GroupId = info.GroupId;

			Update(info);
		}

		public MylistGroupListItem(MylistGroupData mylistGroup, PageManager pageManager)
		{
			_PageManager = pageManager;

			Title = mylistGroup.Name;
			Description = mylistGroup.Description;
			GroupId = mylistGroup.Id;
            OptionText = (mylistGroup.GetIsPublic() ? "公開" : "非公開") + $" - {mylistGroup.Count}件";

            ThemeColor = mylistGroup.GetIconType().ToColor();
            if (mylistGroup.ThumbnailUrls != null)
            {
                foreach (var thumbnailUri in mylistGroup.ThumbnailUrls)
                {
                    ImageUrlsSource.Add(thumbnailUri.OriginalString);
                }
            }
        }

		private DelegateCommand _OpenMylistCommand;
		public override ICommand PrimaryCommand
		{
			get
			{
				return _OpenMylistCommand
					?? (_OpenMylistCommand = new DelegateCommand(() =>
					{
						_PageManager.OpenPage(HohoemaPageType.Mylist, 
                            new MylistPagePayload(GroupId).ToParameterString()
                            );
					}
					));
			}
		}

		public void Update(MylistGroupInfo info)
		{
			Title = info.Name;
			Description = info.Description;
			OptionText = (info.IsPublic ? "公開" : "非公開") + $" - {info.PlaylistItems.Count}件";

            ThemeColor = info.IconType.ToColor();

            // ユーザーマイリストの情報はそのままではサムネが取れない
            // マイリスト内の動画からサムネを取得する？
        }

		public string GroupId { get; private set; }

		PageManager _PageManager;
	}

    public class MylistItemsWithTitle : ListWithTitle<IPlayableList>
    {
        public PlaylistOrigin Origin { get; set; }
    }


    public class ListWithTitle<T>
    {
        public string Title { get; set; }
        public IList<T> Items { get; set; }
    }
}
