using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Service
{
	public class AcceptCacheUsaseDialogService
	{
		public AcceptCacheUsaseDialogService()
		{

		}



		public async Task<bool> ShowConfirmAcceptCacheDialog()
		{
			var dialog = new AcceptCacheUsaseDialog()
			{
				DataContext = new AcceptCacheUsaseDialogContext()
			};

			var result = await dialog.ShowAsync();

			if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
			{
				return true;
			}

			return false;
		}

		public async Task ShowAcceptCacheTextDialog()
		{
			var dialog = new AcceptCacheUsaseDialog()
			{
				DataContext = new AcceptCacheUsaseDialogContext( withConfirm:false )
			};


			await dialog.ShowAsync();
		}
	}


	public class AcceptCacheUsaseDialogContext
	{
		public string ComfirmButtonText { get; private set; }

		public AcceptCacheUsaseDialogContext(bool withConfirm = true)
		{
			ComfirmButtonText = withConfirm ? "同意して動画キャッシュを利用" : "";
		}
	}
}
