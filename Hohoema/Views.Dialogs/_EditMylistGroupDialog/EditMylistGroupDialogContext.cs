using I18NPortable;
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
using Hohoema.Models.Repository.Niconico.Mylist;

namespace Hohoema.Dialogs
{
	public class EditMylistGroupDialogContext
	{
		IScheduler _scheduler;

        public static List<IncoTypeVM> IconTypeList { get; private set; }
		public static List<MylistGroupDefaultSort> MylistDefaultSortList { get; private set; }

		static EditMylistGroupDialogContext()
		{
			IconTypeList = new List<IncoTypeVM>();
			foreach (var iconType in ((MylistGroupIconType[])Enum.GetValues(typeof(MylistGroupIconType))))
			{
				IconTypeList.Add(new IncoTypeVM()
				{
					IconType = iconType,
					Color = iconType.ToColor()
				});
			}

			MylistDefaultSortList = Enum.GetValues(typeof(MylistGroupDefaultSort)).Cast<MylistGroupDefaultSort>().ToList();
		}

		public EditMylistGroupDialogContext(MylistGroupEditData data, bool isCreate = false)
		{
			_scheduler = App.Current.Container.Resolve<IScheduler>();
			DialogTitle = isCreate ? "MylistCreate".Translate() : "EditMylist".Translate();
			MylistName = new ReactiveProperty<string>(_scheduler, data.Name)
				.SetValidateAttribute(() => MylistName);

			MylistDescription = new ReactiveProperty<string>(_scheduler, data.Description);
			MylistIconType = new ReactiveProperty<IncoTypeVM>(_scheduler, IconTypeList.Single(x => x.IconType == data.IconType));
			MylistIsPublicIndex = new ReactiveProperty<int>(_scheduler, data.IsPublic ? 0 : 1); // 公開=1 非公開=0
			MylistDefaultSort = new ReactiveProperty<MylistGroupDefaultSort>(_scheduler, data.MylistDefaultSort);

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
				IconType = MylistIconType.Value.IconType,
				IsPublic = MylistIsPublicIndex.Value == 0 ? true : false,
				MylistDefaultSort = MylistDefaultSort.Value
			};
		}

		public ReactiveProperty<bool> CanEditCompletion { get; private set; }

		public string DialogTitle { get; private set; }

		[Required(ErrorMessage = "Please input mylist name!")]
		public ReactiveProperty<string> MylistName { get; private set; }
		public ReactiveProperty<string> MylistDescription { get; private set; }
		public ReactiveProperty<IncoTypeVM> MylistIconType { get; private set; }
		public ReactiveProperty<int> MylistIsPublicIndex { get; private set; }
		public ReactiveProperty<MylistGroupDefaultSort> MylistDefaultSort { get; private set; }


		public ReactiveProperty<string> LastErrorMessage { get; private set; }
	}

	public class IncoTypeVM
	{
		public MylistGroupIconType IconType { get; set; }
		public Color Color { get; set; }
	}
}
