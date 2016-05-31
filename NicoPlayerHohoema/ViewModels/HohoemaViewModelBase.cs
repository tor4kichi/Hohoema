using NicoPlayerHohoema.Models;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	abstract public class HohoemaViewModelBase : ViewModelBase
	{
		public HohoemaViewModelBase(PageManager pageManager)
		{

		}

		abstract public string GetPageTitle();

	}
}
