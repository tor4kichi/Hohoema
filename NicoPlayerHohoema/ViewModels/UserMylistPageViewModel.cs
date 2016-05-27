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

namespace NicoPlayerHohoema.ViewModels
{
	public class UserMylistPageViewModel : ViewModelBase
	{
		public UserMylistPageViewModel(HohoemaApp app, PageManager pageMaanger)
		{
			_HohoemaApp = app;
			_PageManager = pageMaanger;

			MylistGroupItems = new ObservableCollection<MylistGroupListItem>();
			IsPublicMylist = new ReactiveProperty<bool>(false);
			IsHostedMylist = new ReactiveProperty<bool>(false);
		}

		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);

			List<MylistGroupData> mylists = null;

			if (e.Parameter is string)
			{
				UserId = e.Parameter as string;				
			}

			if (UserId != null)
			{
				try
				{
					var userInfo = await _HohoemaApp.NiconicoContext.User.GetUserAsync(UserId);
					UserName = userInfo.Nickname;
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);
				}

				

				try
				{
					mylists = await _HohoemaApp.ContentFinder.GetUserMylistGroups(UserId);
					IsPublicMylist.Value = true;
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);
					IsPublicMylist.Value = false;
				}

				IsHostedMylist.Value = false;
			}
			else
			{
			
				try
				{
					var userInfo = await _HohoemaApp.NiconicoContext.User.GetInfoAsync();
					UserName = userInfo.Name;
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);

				}

				var torima = new MylistGroupData()
				{
					Id = "0",
					Name = "とりあえずマイリスト"
				};
				mylists = await _HohoemaApp.ContentFinder.GetLoginUserMylistGroups();

				mylists.Insert(0, torima);

				IsHostedMylist.Value = true;
				IsHostedMylist.Value = true;
			}

			var mylistGroupVMItems = mylists.Select(x => new MylistGroupListItem(x, _PageManager));

			MylistGroupItems.Clear();

			foreach (var mylistGroupVM in mylistGroupVMItems)
			{
				MylistGroupItems.Add(mylistGroupVM);
			}
		}

		public ReactiveProperty<bool> IsHostedMylist { get; private set; }
		public ReactiveProperty<bool> IsPublicMylist { get; private set; }

		public string UserId { get; private set; }

		private string _UserName;
		public string UserName
		{
			get { return _UserName; }
			set { SetProperty(ref _UserName, value); }
		}

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;


		public ObservableCollection<MylistGroupListItem> MylistGroupItems { get; private set; }
	}



	public class MylistGroupListItem : BindableBase
	{

		public MylistGroupListItem(MylistGroupData mylistGroup, PageManager pageManager)
		{
			_PageManager = pageManager;

			Title = mylistGroup.Name;
			Description = mylistGroup.Description;
			GroupId = mylistGroup.Id;
			IsPublic = mylistGroup.IsPublic;
			
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

		public bool IsPublic { get; set; }

		public string Title { get; set; }

		public string Description { get; set; }

		public string GroupId { get; private set; }


		PageManager _PageManager;
	}
}
