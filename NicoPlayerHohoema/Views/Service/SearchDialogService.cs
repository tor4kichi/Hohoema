using Microsoft.Practices.Unity;
using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Service
{
	public class SearchDialogService : ISearchDialogService
	{

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;

		public SearchDialogService(HohoemaApp app, PageManager pageManager)
		{
			_HohoemaApp = app;
			_PageManager = pageManager;
		}

		public async Task ShowAsync()
		{
			var contentDialog = new SearchContentDialog()
			{
				DataContext = new ViewModels.SearchViewModel(_HohoemaApp, _PageManager)
			};
			

			await contentDialog.ShowAsync();

			if (contentDialog.DataContext is IDisposable)
			{
				(contentDialog.DataContext as IDisposable).Dispose();
			}
		}
	}
}
