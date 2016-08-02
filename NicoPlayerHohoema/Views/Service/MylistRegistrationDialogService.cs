using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
					return context.GetResult();
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
					return context.GetResult().Item1;
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
			SelectableItems = mylistManager.UserMylists
				.Where(x => hideMylistGroupId == x.GroupId)
				.ToList();

			SelectedItem = new ReactiveProperty<MylistGroupInfo>();
			MylistComment = new ReactiveProperty<string>("");

			IsSelectedItem = SelectedItem
				.Select(x => x != null)
				.ToReactiveProperty();

			IsVisibleMylistComment = true;
		}

		public void Dispose()
		{
			SelectedItem?.Dispose();
			MylistComment?.Dispose();
			IsSelectedItem?.Dispose();
		}

		public Tuple<MylistGroupInfo, string> GetResult()
		{
			return new Tuple<MylistGroupInfo, string>(SelectedItem.Value, MylistComment.Value);
		}

		public bool IsVisibleMylistComment { get; set; }

		public List<MylistGroupInfo> SelectableItems { get; private set; }

		public ReactiveProperty<MylistGroupInfo> SelectedItem { get; private set; }
		public ReactiveProperty<string> MylistComment { get; private set; }

		public ReactiveProperty<bool> IsSelectedItem { get; private set; }


	}

}
