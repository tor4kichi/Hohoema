using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.ViewModels
{
	public class PortalPageViewModel : HohoemaViewModelBase
	{
		public PortalPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager)
		{
			
		}
	}

	abstract public class PotalPageContentViewModel : BindableBase, IDisposable
	{
		public PotalPageContentViewModel(PageManager pageManager)
		{
			PageManager = pageManager;
			_Disposer = new CompositeDisposable();

			var dispatcher = Window.Current.CoreWindow.Dispatcher;
			pageManager.ObserveProperty(x => x.CurrentPageType, isPushCurrentValueAtFirst: true)
				.Subscribe(async x =>
				{
					if (x == HohoemaPageType.Portal)
					{
						await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
						{
							NavigateTo();
						});
					}
					else
					{
						await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
						{
							NavigateFrom();
						});
					}
				})
				.AddTo(_Disposer);
				
		}

		protected virtual void NavigateTo()
		{

		}

		protected virtual void NavigateFrom()
		{

		}

		public void Dispose()
		{
			_Disposer.Dispose();
		}

		private CompositeDisposable _Disposer;
		public PageManager PageManager { get; private set; }

	}
}
