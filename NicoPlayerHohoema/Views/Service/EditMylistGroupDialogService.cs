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

namespace NicoPlayerHohoema.Views.Service
{
	public class EditMylistGroupDialogService
	{
		public EditMylistGroupDialogService()
		{

		}

		public Task<bool> ShowAsync(MylistGroupEditData data)
		{
			return _ShowAsync(data, false);

		}
		public Task<bool> ShowAsyncWithCreateMode(MylistGroupEditData data)
		{
			return _ShowAsync(data, true);
		}

		private async Task<bool> _ShowAsync(MylistGroupEditData data, bool isCreate)
		{
			var context = new EditMylistGroupDialogContext(data, isCreate);
			var dialog = new EditMylistGroupDialog()
			{
				DataContext = context
			};

			var result = await dialog.ShowAsync();

			if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
			{
				var resultData = context.GetResult();
				data.Name = resultData.Name;
				data.Description = resultData.Description;
				data.IconType = resultData.IconType;
				data.IsPublic = resultData.IsPublic;
				data.MylistDefaultSort = resultData.MylistDefaultSort;
			}
			return result == Windows.UI.Xaml.Controls.ContentDialogResult.Secondary;
		}
	}

	public class EditMylistGroupDialogContext
	{
		public static List<IconType> IconTypeList { get; private set; }
		public static List<string> MylistDefaultSortList { get; private set; }

		static EditMylistGroupDialogContext()
		{
			IconTypeList = ((IconType[])Enum.GetValues(typeof(IconType))).ToList();
			MylistDefaultSortList = new List<string>()
			{
				"マイリストへの登録が古い順", // = 0
				"マイリストへの登録が新しい順", // = 1
				"メモ昇順",
				"メモ降順",
				"タイトル昇順",
				"タイトル降順",
				"投稿時間が新しい順",
				"投稿時間が古い順",
				"再生数が少ない順",
				"再生数が多い順",
				"コメントが新しい順",
				"コメント数が少ない順",
				"コメント数が多い順",
				"マイリスト数が少ない順",
				"マイリスト数が多い順",
				"動画時間が短い順",
				"動画時間が長い順",
			};

		}

		public EditMylistGroupDialogContext(MylistGroupEditData data, bool isCreate = false)
		{
			DialogTitle = "マイリストを" + (isCreate ? "作成" : "編集");
			MylistName = new ReactiveProperty<string>(data.Name)
				.SetValidateAttribute(() => MylistName);

			MylistDescription = new ReactiveProperty<string>(data.Description);
			MylistIconType = new ReactiveProperty<IconType>(data.IconType);
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
			// TODO: MylistDefaultSortの0番目に対応する
			if (MylistDefaultSortIndex.Value == 0)
			{
				MylistDefaultSortIndex.Value = 1;
			}

			return new MylistGroupEditData()
			{
				Name = MylistName.Value,
				Description = MylistDescription.Value,
				IconType = MylistIconType.Value,
				IsPublic = MylistIsPublicIndex.Value == 0 ? true : false,
				MylistDefaultSort = (MylistDefaultSort)MylistDefaultSortIndex.Value
			};
		}

		public ReactiveProperty<bool> CanEditCompletion { get; private set; }

		public string DialogTitle { get; private set; }

		[Required(ErrorMessage = "Please input mylist name!")]
		public ReactiveProperty<string> MylistName { get; private set; }
		public ReactiveProperty<string> MylistDescription { get; private set; }
		public ReactiveProperty<IconType> MylistIconType { get; private set; }
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
}
