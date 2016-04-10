using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public class MainPageViewModel : ViewModelBase
	{
		private string message = "Hello world";

		public string Message
		{
			get { return this.message; }
			set { this.SetProperty(ref this.message, value); }
		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);
			Debug.WriteLine("MainPageにきた");
		}

		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			base.OnNavigatingFrom(e, viewModelState, suspending);
			Debug.WriteLine("MainPageから去る");
		}
	}
}
