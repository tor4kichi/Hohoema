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


        public ObservableCollection<MylistGroupListItem> SelectedMylistGroupItems { get; private set; }
        public ReactiveProperty<bool> IsSelectionModeEnable { get; private set; }
        public ReactiveProperty<bool> CanEditSelectedMylistGroups { get; private set; }

        public ReactiveCommand<IPlayableList> OpenMylistCommand { get; private set; }
        public DelegateCommand AddMylistGroupCommand { get; private set; }
        public ReactiveCommand RemoveMylistGroupCommand { get; private set; }
        public ReactiveCommand EditMylistGroupCommand { get; private set; }

        public DelegateCommand AddLocalMylistCommand { get; private set; }



        public UserMylistPageViewModel(HohoemaApp app, PageManager pageMaanger)
			: base(app, pageMaanger)
		{
            OtherOwneredMylistManager = app.OtherOwneredMylistManager;

            MylistList = new ObservableCollection<MylistItemsWithTitle>();

			IsSelectionModeEnable = new ReactiveProperty<bool>(false);
			SelectedMylistGroupItems = new ObservableCollection<MylistGroupListItem>();
            IsLoginUserMylist = new ReactiveProperty<bool>(false);

            OpenMylistCommand = IsSelectionModeEnable.Select(x => !x)
				.ToReactiveCommand<IPlayableList>();

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


			CanEditSelectedMylistGroups = SelectedMylistGroupItems.ObserveProperty(x => x.Count)
				.Select(x => x > 0 && !SelectedMylistGroupItems.Any(y => y.GroupId == "0"))
				.ToReactiveProperty(false);

			RemoveMylistGroupCommand = CanEditSelectedMylistGroups
				.ToReactiveCommand();

			RemoveMylistGroupCommand.Subscribe(async _ => 
			{
				// 確認ダイアログ
				foreach (var selectedMylist in SelectedMylistGroupItems)
				{
					await HohoemaApp.UserMylistManager.RemoveMylist(selectedMylist.GroupId);

					await Task.Delay(500);
				}

				await UpdateUserMylist();
			});


			EditMylistGroupCommand = CanEditSelectedMylistGroups
				.ToReactiveCommand(false);

			EditMylistGroupCommand.Subscribe(async _ => 
			{
				var mylistGroupListItem = SelectedMylistGroupItems.FirstOrDefault();
				var selectedMylistGroupId = mylistGroupListItem?.GroupId;

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
							mylistGroupListItem.Update(mylistGroup);
							break;
						}
					}
					else
					{
						break;
					}
				}

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
                    Items = mylists.ToList()
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
                Items = listItems.Cast<IPlayableList>().ToList()
            });
            MylistList.Add(new MylistItemsWithTitle()
            {
                Title = "ローカル",
                Items = HohoemaApp.Playlist.Playlists.Cast<IPlayableList>().ToList()
            });
        }

		


		protected void ClearSelection()
		{
			SelectedMylistGroupItems.Clear();
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

    }


    public class ListWithTitle<T>
    {
        public string Title { get; set; }
        public List<T> Items { get; set; }
    }
}
