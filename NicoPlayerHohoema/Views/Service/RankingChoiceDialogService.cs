using NicoPlayerHohoema.Models;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Service
{
	public class RankingChoiceDialogService
	{
		private RankingChoiceDialogContext _Context;


		public RankingChoiceDialogService()
		{
			_Context = new RankingChoiceDialogContext();
		}

		public async Task<RankingCategoryInfo> ShowDialog(IEnumerable<RankingCategoryInfo> selectableItems)
		{
			_Context.ResetItems(selectableItems);

			var dialog = new Views.RankingChoiceContentDialog()
			{
				DataContext = _Context
			};


			var result = await dialog.ShowAsync();

			if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
			{
				return _Context.GetResult();
			}
			else
			{
				return null;
			}
		}


		public async Task<RankingCategoryInfo> ShowDislikeRankingCategoryChoiceDialog(IEnumerable<RankingCategoryInfo> selectableItems)
		{
			var context = new DislikeRankingChoiceDialogContext();

			context.ResetItems(selectableItems);

			var dialog = new Views.Service.DislikeRankingCategoryChoiceDialog()
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
				else
				{
					return null;
				}
			}
			finally
			{
				(context as IDisposable)?.Dispose();
			}
		}
	}

	public class RankingChoiceDialogContext : BindableBase
	{
		public RankingChoiceDialogContext()
		{
			Items = new ObservableCollection<RankingCategoryInfo>();
			IsCategoryRankingSelected = true;
			IsCustomRankingSelected = false;
			CustomRankingKeyword = "";
		}



		public void ResetItems(IEnumerable<RankingCategoryInfo> selectableItems)
		{
			Items.Clear();

			foreach (var item in selectableItems)
			{
				Items.Add(item);
			}

			SelectedItem = Items.FirstOrDefault();
		}



		public RankingCategoryInfo GetResult()
		{
			if (IsCategoryRankingSelected)
			{
				return SelectedItem;
			}
			else if (IsCustomRankingSelected)
			{
				return new RankingCategoryInfo()
				{
					RankingSource = RankingSource.SearchWithMostPopular,
					Parameter = CustomRankingKeyword,
					DisplayLabel = CustomRankingKeyword,
				};
			}
			else
			{
				throw new Exception();
			}
		}


		

		private bool _IsCategoryRankingSelected;
		public bool IsCategoryRankingSelected
		{
			get { return _IsCategoryRankingSelected; }
			set { SetProperty(ref _IsCategoryRankingSelected, value); }
		}

		private bool _IsCustomRankingSelected;
		public bool IsCustomRankingSelected
		{
			get { return _IsCustomRankingSelected; }
			set { SetProperty(ref _IsCustomRankingSelected, value); }
		}


		public ObservableCollection<RankingCategoryInfo> Items { get; private set; }

		public RankingCategoryInfo SelectedItem { get; set; }


		private string _CustomRankingKeyword;
		public string CustomRankingKeyword
		{
			get { return _CustomRankingKeyword; }
			set { SetProperty(ref _CustomRankingKeyword, value); }
		}
	}


	public class DislikeRankingChoiceDialogContext : BindableBase, IDisposable
	{
		public DislikeRankingChoiceDialogContext()
		{
			Items = new ObservableCollection<RankingCategoryInfo>();
			SelectedItem = new ReactiveProperty<RankingCategoryInfo>();

			SelectedItem.Subscribe(x => IsSelectedItem = x != null);
		}



		public void ResetItems(IEnumerable<RankingCategoryInfo> selectableItems)
		{
			Items.Clear();

			foreach (var item in selectableItems)
			{
				Items.Add(item);
			}

			SelectedItem.Value = null;
		}



		public RankingCategoryInfo GetResult()
		{
			return SelectedItem.Value;
		}

		public void Dispose()
		{
			SelectedItem?.Dispose();
		}

		public ObservableCollection<RankingCategoryInfo> Items { get; private set; }

		public ReactiveProperty<RankingCategoryInfo> SelectedItem { get; set; }


		private bool _IsSelectedItem;
		public bool IsSelectedItem
		{
			get { return _IsSelectedItem; }
			set { SetProperty(ref _IsSelectedItem, value); }
		}
	}
}
