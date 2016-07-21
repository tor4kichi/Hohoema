using NicoPlayerHohoema.ViewModels;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Service
{
	public class EditAutoCacheConditionDialogService
	{

		public async Task<bool> ShowDialog(AutoCacheConditionViewModel vm)
		{
			var dialog = new Views.EditAutCacheConditionDialog()
			{
				DataContext = vm
			};

			var result = await dialog.ShowAsync();
			return result == Windows.UI.Xaml.Controls.ContentDialogResult.Secondary;
		}
	}

	
}
