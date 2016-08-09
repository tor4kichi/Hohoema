using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;

namespace NicoPlayerHohoema.ViewModels
{
	public class EmptySearchPageContentViewModel : HohoemaViewModelBase
	{
		public EmptySearchPageContentViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
			: base(hohoemaApp, pageManager, isRequireSignIn:false)
		{
		}
	}
}
