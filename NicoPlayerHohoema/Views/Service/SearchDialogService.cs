using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Service
{
	public class SearchDialogService : ISearchDialogService
	{
		public object DataContext { get; private set; }

		public SearchDialogService(object dataContext)
		{
			DataContext = dataContext;
		}

		public async Task ShowAsync()
		{
			var contentDialog = new SearchContentDialog()
			{
				DataContext = DataContext
			};

			await contentDialog.ShowAsync();
		}
	}
}
