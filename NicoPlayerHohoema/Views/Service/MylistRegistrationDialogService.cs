using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Service
{
	public class MylistRegistrationDialogService
	{
		private HohoemaApp _HohoemaApp;

		public MylistRegistrationDialogService(HohoemaApp app)
		{
			_HohoemaApp = app;
		}

		public async Task<Tuple<MylistGroupInfo, string>> ShowDialog(IReadOnlyList<ViewModels.VideoInfoControlViewModel> videos)
		{
			var mylistManager = _HohoemaApp.UserMylistManager;
			var context = new MylistRegistrationDialogContext(mylistManager, videos.Count, hideMylistGroupId : null);

			var dialog = new Views.Service.MylistRegistrationDialog()
			{
				DataContext = context
			};

			try
			{
				var result = await dialog.ShowAsync();

				if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
				{
					return await context.GetResult();
				}
			}
			finally
			{
				context.Dispose();
			}

			return null;
		}


		public async Task<MylistGroupInfo> ShowSelectSingleMylistDialog(IReadOnlyList<ViewModels.VideoInfoControlViewModel> videos, string hideMylistGroupId = null)
		{
			var mylistManager = _HohoemaApp.UserMylistManager;
			var context = new MylistRegistrationDialogContext(mylistManager, videos.Count, hideMylistGroupId);

			// マイリストコメントは利用しない
			context.IsVisibleMylistComment = false;

			var dialog = new Views.Service.MylistRegistrationDialog()
			{
				DataContext = context
			};

			dialog.SecondaryButtonText = "選択";

			try
			{
				var result = await dialog.ShowAsync();

				if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
				{
					var tuple = await context.GetResult();
					return tuple.Item1;
				}
			}
			finally
			{
				context.Dispose();
			}

			return null;
		}
	}


	public class MylistRegistrationDialogContext : IDisposable
	{
		public MylistRegistrationDialogContext(UserMylistManager mylistManager, int videoCount, string hideMylistGroupId)
		{
			UserMylistManager = mylistManager;
			_CompositeDisposable = new CompositeDisposable();


			IsCreateMylistMode = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			if (hideMylistGroupId == null)
			{
				SelectableItems = mylistManager.UserMylists.ToList();
			}
			else
			{
				SelectableItems = mylistManager.UserMylists
					.Where(x => hideMylistGroupId == x.GroupId)
					.ToList();
			}

			SelectedItem = new ReactiveProperty<MylistGroupInfo>(SelectableItems[0])
				.AddTo(_CompositeDisposable);
			MylistComment = new ReactiveProperty<string>("")
				.AddTo(_CompositeDisposable);

			IsSelectedItem = SelectedItem
				.Select(x => x != null)
				.ToReactiveProperty()
				.AddTo(_CompositeDisposable);

			NewMylistName = new ReactiveProperty<string>("新しいマイリスト")
				.AddTo(_CompositeDisposable);
			HasMylistName = NewMylistName.Select(x => x.Length > 0)
				.ToReactiveProperty()
				.AddTo(_CompositeDisposable);

			CanCompleteSelection =
				Observable.Merge(
					IsCreateMylistMode.ToUnit(),
					IsSelectedItem.ToUnit(),
					HasMylistName.ToUnit()
					)
					.Select(_ =>
					{
						if (IsCreateMylistMode.Value)
						{
							return HasMylistName.Value;
						}
						else
						{
							return IsSelectedItem.Value;
						}
					})
					.ToReactiveProperty(false)
				.AddTo(_CompositeDisposable);

			CanAddMylist = false;//UserMylistManager.CanAddMylist;
			IsVisibleMylistComment = true;
		}

		public void Dispose()
		{
			_CompositeDisposable?.Dispose();
		}

		public async Task<Tuple<MylistGroupInfo, string>> GetResult()
		{
			MylistGroupInfo mylistGroupInfo;
			if (IsCreateMylistMode.Value)
			{
				if (HasMylistName.Value == false)
				{
					throw new Exception();
				}
				var prevMylistGroups = UserMylistManager.UserMylists.ToArray();

				var result = await UserMylistManager.AddMylist(NewMylistName.Value, "", false, Mntone.Nico2.Mylist.MylistDefaultSort.Latest, Mntone.Nico2.Mylist.IconType.Default);

				if (result == Mntone.Nico2.ContentManageResult.Success)
				{
					var added = UserMylistManager.UserMylists.Single(x => !prevMylistGroups.Any(y => x.GroupId == y.GroupId));
					mylistGroupInfo = added;
				}
				else
				{
					throw new Exception();
				}
			}
			else
			{
				mylistGroupInfo = SelectedItem.Value;
			}
			return new Tuple<MylistGroupInfo, string>(mylistGroupInfo, MylistComment.Value);
		}

		public bool IsVisibleMylistComment { get; set; }

		public ReactiveProperty<bool> IsCreateMylistMode { get; private set; }

		public ReactiveProperty<bool> CanCompleteSelection { get; private set; }

		public List<MylistGroupInfo> SelectableItems { get; private set; }
		public ReactiveProperty<MylistGroupInfo> SelectedItem { get; private set; }
		public ReactiveProperty<bool> IsSelectedItem { get; private set; }



		public ReactiveProperty<string> NewMylistName { get; private set; }
		public ReactiveProperty<bool> HasMylistName { get; private set; }

		public ReactiveProperty<string> MylistComment { get; private set; }

		public bool CanAddMylist { get; private set; }

		public UserMylistManager UserMylistManager { get; private set; }

		private CompositeDisposable _CompositeDisposable;
	}

}
