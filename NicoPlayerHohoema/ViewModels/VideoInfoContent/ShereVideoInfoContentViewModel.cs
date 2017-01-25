using Microsoft.Toolkit.Uwp.Services.Twitter;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Util;
using NicoPlayerHohoema.ViewModels;
using NicoPlayerHohoema.Views.Service;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.VideoInfoContent
{
	public sealed class ShereVideoInfoContentViewModel: MediaInfoViewModel
	{
		TextInputDialogService _TextInputDialogService;
		ToastNotificationService _ToastNotificationService;

		NicoVideo _NicoVideo;

		public ShereVideoInfoContentViewModel(NicoVideo nicoVideo, TextInputDialogService textInputService, ToastNotificationService toastService)
		{
			_NicoVideo = nicoVideo;
			_TextInputDialogService = textInputService;
			_ToastNotificationService = toastService;

			
		}


		
	}
}
