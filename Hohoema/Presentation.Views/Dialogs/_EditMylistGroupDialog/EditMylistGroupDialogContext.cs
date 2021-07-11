using I18NPortable;
using Hohoema.Models.Domain;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Prism.Ioc;
using NiconicoToolkit.Mylist;
using Hohoema.Models.Domain.Niconico.Mylist;

namespace Hohoema.Dialogs
{
	public sealed class EditMylistGroupDialogContext : IDisposable
	{
		IScheduler _scheduler;

		public MylistPlaylistSortOption[] SortItems => MylistPlaylist.SortOptions;


		public EditMylistGroupDialogContext(MylistGroupEditData data, bool isCreate = false)
		{
			_scheduler = App.Current.Container.Resolve<IScheduler>();
			DialogTitle = isCreate ? "MylistCreate".Translate() : "EditMylist".Translate();
			MylistName = new ReactiveProperty<string>(_scheduler, data.Name)
				.SetValidateAttribute(() => MylistName);

			MylistDescription = new ReactiveProperty<string>(_scheduler, data.Description);
			MylistIsPublicIndex = new ReactiveProperty<int>(_scheduler, data.IsPublic ? 1 : 0); // 公開=0 非公開=1
			SelectedSort = new ReactiveProperty<MylistPlaylistSortOption>(_scheduler, SortItems.First(x => x.SortKey == data.DefaultSortKey && x.SortOrder == data.DefaultSortOrder));

			CanEditCompletion = MylistName.ObserveHasErrors
				.Select(x => !x)
				.ToReactiveProperty(raiseEventScheduler: _scheduler);

			LastErrorMessage = MylistName.ObserveErrorChanged
				.Select(x => x?.OfType<string>().FirstOrDefault())
				.ToReactiveProperty(raiseEventScheduler: _scheduler);
			

		}

		public MylistGroupEditData GetResult()
		{
			return new MylistGroupEditData()
			{
				Name = MylistName.Value,
				Description = MylistDescription.Value,
				IsPublic = MylistIsPublicIndex.Value == 1 ? true : false,
				DefaultSortKey = SelectedSort.Value.SortKey,
				DefaultSortOrder = SelectedSort.Value.SortOrder,
			};
		}

        public void Dispose()
        {
			CanEditCompletion?.Dispose();
			MylistName?.Dispose();
            MylistDescription?.Dispose();
            MylistIsPublicIndex?.Dispose();
            SelectedSort?.Dispose();
            LastErrorMessage?.Dispose();
        }

        public ReactiveProperty<bool> CanEditCompletion { get; private set; }

		public string DialogTitle { get; private set; }

		[Required(ErrorMessage = "Please input mylist name!")]
		public ReactiveProperty<string> MylistName { get; private set; }
		public ReactiveProperty<string> MylistDescription { get; private set; }
		public ReactiveProperty<int> MylistIsPublicIndex { get; private set; }
		public ReactiveProperty<MylistPlaylistSortOption> SelectedSort { get; private set; }


		public ReactiveProperty<string> LastErrorMessage { get; private set; }
	}

	public class MylistGroupEditData
	{
		public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
		public bool IsPublic { get; set; }
		public MylistSortKey DefaultSortKey { get; set; }
		public MylistSortOrder DefaultSortOrder { get; set; }

		public MylistGroupEditData()
		{

		}
	}
}
