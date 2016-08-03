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

namespace NicoPlayerHohoema.ViewModels
{
	public class UserMylistPageViewModel : HohoemaViewModelBase
	{
		public UserMylistPageViewModel(HohoemaApp app, PageManager pageMaanger)
			: base(app, pageMaanger)
		{
			MylistGroupItems = new ObservableCollection<MylistGroupListItem>();
			IsSelectionModeEnable = new ReactiveProperty<bool>(false);
			SelectedMylistGroupItems = new ObservableCollection<MylistGroupListItem>();

			OpenMylistCommand = IsSelectionModeEnable.Select(x => !x)
				.ToReactiveCommand<MylistGroupListItem>();

			OpenMylistCommand.Subscribe(listItem => 
			{
				ClearSelection();
				PageManager.OpenPage(HohoemaPageType.Mylist, listItem.GroupId);
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
							UpdateUserMylist();
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

				UpdateUserMylist();
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
		}


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);
		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			List<MylistGroupData> mylists = null;

			if (e.Parameter is string)
			{
				UserId = e.Parameter as string;				
			}

			MylistGroupItems.Clear();

			if (UserId != null)
			{
				try
				{
					var userInfo = await HohoemaApp.NiconicoContext.User.GetUserAsync(UserId);
					UserName = userInfo.Nickname;
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);
				}

				

				try
				{
					mylists = await HohoemaApp.ContentFinder.GetUserMylistGroups(UserId);
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);
				}

				var mylistGroupVMItems = mylists.Select(x => new MylistGroupListItem(x, PageManager));

				foreach (var mylistGroupVM in mylistGroupVMItems)
				{
					MylistGroupItems.Add(mylistGroupVM);
				}
			}
			else
			{
				// ログインユーザーのマイリスト一覧を表示
				UserName = HohoemaApp.LoginUserName;

				UpdateUserMylist();
			}

			
			UpdateTitle($"{UserName} さんのマイリスト一覧");

			AddMylistGroupCommand.RaiseCanExecuteChanged();
		}


		private void UpdateUserMylist()
		{
			MylistGroupItems.Clear();

			var listItems = HohoemaApp.UserMylistManager.UserMylists
				.Select(x => new MylistGroupListItem(x, PageManager));

			foreach (var item in listItems)
			{
				MylistGroupItems.Add(item);
			}

		}

		protected void ClearSelection()
		{
			SelectedMylistGroupItems.Clear();
		}


		public ReactiveCommand<MylistGroupListItem> OpenMylistCommand { get; private set; }

		public DelegateCommand AddMylistGroupCommand { get; private set; }
		public ReactiveCommand RemoveMylistGroupCommand { get; private set; }

		public ReactiveCommand EditMylistGroupCommand { get; private set; }


		public string UserId { get; private set; }

		private string _UserName;
		public string UserName
		{
			get { return _UserName; }
			set { SetProperty(ref _UserName, value); }
		}


		public ReactiveProperty<bool> IsSelectionModeEnable { get; private set; }

		public ObservableCollection<MylistGroupListItem> SelectedMylistGroupItems { get; private set; }

		public ReactiveProperty<bool> CanEditSelectedMylistGroups { get; private set;}


		public ObservableCollection<MylistGroupListItem> MylistGroupItems { get; private set; }

		public UserMylistManager UserMylistManager { get; private set; }
	}



	public class MylistGroupListItem : BindableBase
	{
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
			IsPublic = mylistGroup.GetIsPublic();
			ItemCount = mylistGroup.Count;
			ThemeColor = mylistGroup.GetIconType().ToColor();
		}

		private DelegateCommand _OpenMylistCommand;
		public DelegateCommand OpenMylistCommand
		{
			get
			{
				return _OpenMylistCommand
					?? (_OpenMylistCommand = new DelegateCommand(() =>
					{
						_PageManager.OpenPage(HohoemaPageType.Mylist, GroupId);
					}
					));
			}
		}

		public void Update(MylistGroupInfo info)
		{
			Title = info.Name;
			Description = info.Description;
			IsPublic = info.IsPublic;
			ItemCount = info.VideoItems.Count;
			ThemeColor = info.IconType.ToColor();


			OnPropertyChanged(nameof(Title));
			OnPropertyChanged(nameof(Description));
			OnPropertyChanged(nameof(IsPublic));
			OnPropertyChanged(nameof(ItemCount));
			OnPropertyChanged(nameof(ThemeColor));
		}

		public bool IsPublic { get; set; }

		public string Title { get; set; }

		public string Description { get; set; }

		public string GroupId { get; private set; }

		public int ItemCount { get; private set; }

		public Color ThemeColor { get; private set; }


		PageManager _PageManager;
	}
}
