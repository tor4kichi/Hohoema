using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NicoPlayerHohoema.ViewModels
{
	public abstract class HohoemaListingPageItemBase : BindableBase, IDisposable
	{
		public abstract ICommand SelectedCommand { get; }

		public abstract void Dispose();
	}
}
