using I18NPortable;
using Mntone.Nico2.Mylist;
using NicoPlayerHohoema.Models;
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

namespace NicoPlayerHohoema.Dialogs
{
	public class EditMylistGroupDialogContext
	{
		IScheduler _scheduler;

        public static List<IncoTypeVM> IconTypeList { get; private set; }
		public static List<MylistDefaultSort> MylistDefaultSortList { get; private set; }

		static EditMylistGroupDialogContext()
		{
			IconTypeList = new List<IncoTypeVM>();
			foreach (var iconType in ((IconType[])Enum.GetValues(typeof(IconType))))
			{
				IconTypeList.Add(new IncoTypeVM()
				{
					IconType = iconType,
					Color = iconType.ToColor()
				});
			}

			MylistDefaultSortList = Enum.GetValues(typeof(MylistDefaultSort)).Cast<MylistDefaultSort>().ToList();
		}

		public EditMylistGroupDialogContext(MylistGroupEditData data, bool isCreate = false)
		{
			_scheduler = App.Current.Container.Resolve<IScheduler>();
			DialogTitle = isCreate ? "MylistCreate".Translate() : "EditMylist".Translate();
			MylistName = new ReactiveProperty<string>(_scheduler, data.Name)
				.SetValidateAttribute(() => MylistName);

			MylistDescription = new ReactiveProperty<string>(_scheduler, data.Description);
			MylistIsPublicIndex = new ReactiveProperty<int>(_scheduler, data.IsPublic ? 0 : 1); // 公開=1 非公開=0
			SortKey = new ReactiveProperty<MylistSortKey>(_scheduler, data.DefaultSortKey);
			SortOrder = new ReactiveProperty<MylistSortOrder>(_scheduler, data.DefaultSortOrder);

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
				IsPublic = MylistIsPublicIndex.Value == 0 ? true : false,

			};
		}

		public ReactiveProperty<bool> CanEditCompletion { get; private set; }

		public string DialogTitle { get; private set; }

		[Required(ErrorMessage = "Please input mylist name!")]
		public ReactiveProperty<string> MylistName { get; private set; }
		public ReactiveProperty<string> MylistDescription { get; private set; }
		public ReactiveProperty<int> MylistIsPublicIndex { get; private set; }
		public ReactiveProperty<MylistSortKey> SortKey { get; private set; }
		public ReactiveProperty<MylistSortOrder> SortOrder { get; private set; }


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
