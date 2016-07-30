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


		public void ShowText(string title, string content, bool isSuppress = false)
		{
			var text = $@"
				<toast>
					<visual>
						<binding >
							<text id=""1"">{title}</text>
							<text id=""2"">{content}</text>
						</binding>  
					</visual>
				</toast>"
				;
			var docu = new XmlDocument();
			docu.LoadXml(text);
			var toast = new ToastNotification(docu);
			toast.ExpirationTime = DateTime.Now.AddSeconds(5);
			toast.SuppressPopup = isSuppress;
			
			_Nofifier.Show(toast);
		}
		


	}
}
