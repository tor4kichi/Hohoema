using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Notifications;
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


		public void ShowText(string title, string content, ToastDuration duration = ToastDuration.Short, bool isSuppress = false, string luanchContent = null, Action toastActivatedAction = null)
		{
			var toust = new ToastContent();
			toust.Visual = new ToastVisual()
			{
				BindingGeneric = new ToastBindingGeneric()
				{
					Children =
					{
						new AdaptiveText()
						{
							Text = title
						},

						new AdaptiveText()
						{
							Text = content,
							
						},

						
					}
				}
			};

			toust.Launch = luanchContent;
			toust.Duration = duration;
			

			var toast = new ToastNotification(toust.GetXml());
			toast.SuppressPopup = isSuppress;

			if (toastActivatedAction != null)
			{
				toast.Activated += (ToastNotification sender, object args) => toastActivatedAction();
			}
			
			_Nofifier.Show(toast);
		}
	}
}
