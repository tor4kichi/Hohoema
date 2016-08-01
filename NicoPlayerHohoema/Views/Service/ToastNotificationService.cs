using NotificationsExtensions.Toasts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace NicoPlayerHohoema.Views.Service
{
	public class ToastNotificationService
	{
		ToastNotifier _Nofifier;
		public ToastNotificationService()
		{
			_Nofifier = ToastNotificationManager.CreateToastNotifier();
		}


		public void ShowText(string title, string content, ToastDuration duration = ToastDuration.Short, bool isSuppress = false)
		{
			var toastContent = new ToastContent();

			toastContent.Visual = new ToastVisual()
			{
				BindingGeneric = new ToastBindingGeneric()
				{
					Attribution = new ToastGenericAttributionText()
					{
						Text = title + "\n" + content
					}
				}
			};
			
			toastContent.Duration = ToastDuration.Long;
			
			var toast = new ToastNotification(toastContent.GetXml());
			toast.SuppressPopup = isSuppress;
			
			_Nofifier.Show(toast);
		}
		


	}
}
