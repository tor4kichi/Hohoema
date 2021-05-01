using I18NPortable;
using Mntone.Nico2.Mylist;
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
using Mntone.Nico2.Users.Mylist;
using Hohoema.Presentation.ViewModels.Niconico.Mylist;

namespace Hohoema.Dialogs
{
	public class EditMylistGroupDialogContext
	{
		IScheduler _scheduler;

		static public MylistSortViewModel[] SortItems { get; } = new MylistSortViewModel[]
		{
			new MylistSortViewModel() { Key = MylistSortKey.AddedAt, Order = MylistSortOrder.Desc },
			new MylistSortViewModel() { Key = MylistSortKey.AddedAt, Order = MylistSortOrder.Asc },
			new MylistSortViewModel() { Key = MylistSortKey.Title, Order = MylistSortOrder.Asc },
			new MylistSortViewModel() { Key = MylistSortKey.Title, Order = MylistSortOrder.Desc },
			new MylistSortViewModel() { Key = MylistSortKey.MylistComment, Order = MylistSortOrder.Asc },
			new MylistSortViewModel() { Key = MylistSortKey.MylistComment, Order = MylistSortOrder.Desc },
			new MylistSortViewModel() { Key = MylistSortKey.RegisteredAt, Order = MylistSortOrder.Desc },
			new MylistSortViewModel() { Key = MylistSortKey.RegisteredAt, Order = MylistSortOrder.Asc },
			new MylistSortViewModel() { Key = MylistSortKey.ViewCount, Order = MylistSortOrder.Desc },
			new MylistSortViewModel() { Key = MylistSortKey.ViewCount, Order = MylistSortOrder.Asc },
			new MylistSortViewModel() { Key = MylistSortKey.LastCommentTime, Order = MylistSortOrder.Desc },
			new MylistSortViewModel() { Key = MylistSortKey.LastCommentTime, Order = MylistSortOrder.Asc },
			new MylistSortViewModel() { Key = MylistSortKey.CommentCount, Order = MylistSortOrder.Desc },
			new MylistSortViewModel() { Key = MylistSortKey.CommentCount, Order = MylistSortOrder.Asc },
			new MylistSortViewModel() { Key = MylistSortKey.MylistCount, Order = MylistSortOrder.Desc },
			new MylistSortViewModel() { Key = MylistSortKey.MylistCount, Order = MylistSortOrder.Asc },
			new MylistSortViewModel() { Key = MylistSortKey.Duration, Order = MylistSortOrder.Desc },
			new MylistSortViewModel() { Key = MylistSortKey.Duration, Order = MylistSortOrder.Asc },
		};


		public EditMylistGroupDialogContext(MylistGroupEditData data, bool isCreate = false)
		{
			_scheduler = App.Current.Container.Resolve<IScheduler>();
			DialogTitle = isCreate ? "MylistCreate".Translate() : "EditMylist".Translate();
			MylistName = new ReactiveProperty<string>(_scheduler, data.Name)
				.SetValidateAttribute(() => MylistName);

			MylistDescription = new ReactiveProperty<string>(_scheduler, data.Description);
			MylistIsPublicIndex = new ReactiveProperty<int>(_scheduler, data.IsPublic ? 1 : 0); // 公開=0 非公開=1
			SelectedSort = new ReactiveProperty<MylistSortViewModel>(_scheduler, SortItems.First(x => x.Key == data.DefaultSortKey && x.Order == data.DefaultSortOrder));

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
				DefaultSortKey = SelectedSort.Value.Key,
				DefaultSortOrder = SelectedSort.Value.Order,
			};
		}

		public ReactiveProperty<bool> CanEditCompletion { get; private set; }

		public string DialogTitle { get; private set; }

		[Required(ErrorMessage = "Please input mylist name!")]
		public ReactiveProperty<string> MylistName { get; private set; }
		public ReactiveProperty<string> MylistDescription { get; private set; }
		public ReactiveProperty<int> MylistIsPublicIndex { get; private set; }
		public ReactiveProperty<MylistSortViewModel> SelectedSort { get; private set; }


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


	public class IncoTypeVM
	{
		public IconType IconType { get; set; }
		public Color Color { get; set; }
	}
}
