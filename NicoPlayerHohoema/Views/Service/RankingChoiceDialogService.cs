using NicoPlayerHohoema.Models;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
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

		public RankingChoiceDialogService()
		{
		}

		

		public async Task<List<RankingCategoryInfo>> ShowRankingCategoryChoiceDialog(
            string title, 
            IEnumerable<RankingCategoryInfo> selectableItems, 
            IEnumerable<RankingCategoryInfo> selectedItems
            )
		{
			var context = new RankingChoiceDialogContext(title);

			context.ResetItems(selectableItems);

            foreach (var item in selectedItems)
            {
                context.SelectedItems.Add(item);
            }

            var dialog = new Views.Service.DislikeRankingCategoryChoiceDialog()
			{
				DataContext = context
			};

			try
			{
				var result = await dialog.ShowAsync();

				if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
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

	


	public class RankingChoiceDialogContext : BindableBase, IDisposable
	{
		public RankingChoiceDialogContext(string title)
		{
			Title = title;
			Items = new ObservableCollection<RankingCategoryInfo>();
			SelectedItems = new ObservableCollection<RankingCategoryInfo>();

			_SelectedItemsCountObserver = SelectedItems.ObserveProperty(x => x.Count).Subscribe(x => IsSelectedItem = x > 0);
		}



		public void ResetItems(IEnumerable<RankingCategoryInfo> selectableItems)
		{
			Items.Clear();

			foreach (var item in selectableItems)
			{
				Items.Add(item);
			}

			SelectedItems.Clear();
		}



		public List<RankingCategoryInfo> GetResult()
		{
			return SelectedItems.ToList();
		}

		public void Dispose()
		{
			_SelectedItemsCountObserver?.Dispose();
		}

		public string Title { get; private set; }
		public ObservableCollection<RankingCategoryInfo> Items { get; private set; }

		public ObservableCollection<RankingCategoryInfo> SelectedItems { get; set; }

		IDisposable _SelectedItemsCountObserver;

		private bool _IsSelectedItem;
		public bool IsSelectedItem
		{
			get { return _IsSelectedItem; }
			set { SetProperty(ref _IsSelectedItem, value); }
		}
	}
}
