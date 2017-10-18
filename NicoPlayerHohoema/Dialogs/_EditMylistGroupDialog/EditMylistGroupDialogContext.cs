using Mntone.Nico2.Mylist;
using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace NicoPlayerHohoema.Dialogs
{
	public class EditMylistGroupDialogContext
	{
		public static List<IncoTypeVM> IconTypeList { get; private set; }
		public static List<string> MylistDefaultSortList { get; private set; }

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

			MylistDefaultSortList = new List<string>()
			{
				"登録が古い順", // = 0
				"登録が新しい順", // = 1
				"メモ昇順",
				"メモ降順",
				"タイトル昇順",
				"タイトル降順",
				"投稿が新しい順",
				"投稿が古い順",
				"再生が多い順",
				"再生が少ない順",
				"コメントが新しい順",
				"コメントが古い順",
				"コメント数が多い順",
				"コメント数が少ない順",
				"マイリストに追加が多い順",
				"マイリストに追加が少ない順",
				"動画時間が長い順",
				"動画時間が短い順",
			};

		}

		public EditMylistGroupDialogContext(MylistGroupEditData data, bool isCreate = false)
		{
			DialogTitle = "マイリストを" + (isCreate ? "作成" : "編集");
			MylistName = new ReactiveProperty<string>(data.Name)
				.SetValidateAttribute(() => MylistName);

			MylistDescription = new ReactiveProperty<string>(data.Description);
			MylistIconType = new ReactiveProperty<IncoTypeVM>(IconTypeList.Single(x => x.IconType == data.IconType));
			MylistIsPublicIndex = new ReactiveProperty<int>(data.IsPublic ? 0 : 1); // 公開=1 非公開=0
			MylistDefaultSortIndex = new ReactiveProperty<int>((int)data.MylistDefaultSort);

			CanEditCompletion = MylistName.ObserveHasErrors
				.Select(x => !x)
				.ToReactiveProperty();

			LastErrorMessage = MylistName.ObserveErrorChanged
				.Select(x => x?.OfType<string>().FirstOrDefault())
				.ToReactiveProperty();
			

		}

		public MylistGroupEditData GetResult()
		{
			return new MylistGroupEditData()
			{
				Name = MylistName.Value,
				Description = MylistDescription.Value,
				IconType = MylistIconType.Value.IconType,
				IsPublic = MylistIsPublicIndex.Value == 0 ? true : false,
				MylistDefaultSort = (MylistDefaultSort)MylistDefaultSortIndex.Value
			};
		}

		public ReactiveProperty<bool> CanEditCompletion { get; private set; }

		public string DialogTitle { get; private set; }

		[Required(ErrorMessage = "Please input mylist name!")]
		public ReactiveProperty<string> MylistName { get; private set; }
		public ReactiveProperty<string> MylistDescription { get; private set; }
		public ReactiveProperty<IncoTypeVM> MylistIconType { get; private set; }
		public ReactiveProperty<int> MylistIsPublicIndex { get; private set; }
		public ReactiveProperty<int> MylistDefaultSortIndex { get; private set; }


		public ReactiveProperty<string> LastErrorMessage { get; private set; }
	}

	public class MylistGroupEditData
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public IconType IconType { get; set; }
		public bool IsPublic { get; set; }
		public MylistDefaultSort MylistDefaultSort { get; set; }

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
