using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public class PortalPageViewModel : ViewModelBase
	{
		public PortalPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
		{
			
		}
	}

	abstract public class PotalPageContentViewModel : BindableBase
	{
		public PotalPageContentViewModel(PageManager pageManager)
		{
			PageManager = pageManager;

			pageManager.ObserveProperty(x => x.CurrentPageType)
				.Subscribe(x => 
				{
					if (x == HohoemaPageType.Portal)
					{
						NavigateTo();
					}
					else
					{
						NavigateFrom();
					}
				});
		}
		protected virtual void NavigateTo()
		{

		}

		protected virtual void NavigateFrom()
		{

		}

		public PageManager PageManager { get; private set; }

	}
}
