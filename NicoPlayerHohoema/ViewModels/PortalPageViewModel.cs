using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.AppMap;
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
using Prism.Windows.Navigation;
using System.Threading;
using Reactive.Bindings;

namespace NicoPlayerHohoema.ViewModels
{
	public class PortalPageViewModel : HohoemaViewModelBase
	{
		public AppMapContainerViewModel Root { get; private set; }

		public PortalPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager)
		{
			Root = new AppMapContainerViewModel(HohoemaApp.AppMapManager.Root, PageManager);
		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			await HohoemaApp.AppMapManager.Refresh();
		}
	}



	public class AppMapItemViewModel
	{
		public IAppMapItem Item { get; private set; }

		public bool IsHiddenLabel { get; private set; }

		public PageManager PageManager { get; private set; }

		public AppMapItemViewModel(IAppMapItem item, PageManager pageManager)
		{
			Item = item;
			PageManager = pageManager;

			IsHiddenLabel = Item.PageType == HohoemaPageType.Portal;
		}


		private DelegateCommand _OpenItemPageCommand;
		public DelegateCommand OpenItemPageCommand
		{
			get
			{
				return _OpenItemPageCommand
					?? (_OpenItemPageCommand = new DelegateCommand(() => 
					{
						PageManager.OpenPage(Item.PageType, Item.Parameter);
					}));
			}
		}

		
	}

	public class AppMapContainerViewModel : AppMapItemViewModel, IDisposable
	{
		public SelectableAppMapContainerBase Container { get; private set; }

		public ReadOnlyReactiveCollection<AppMapItemViewModel> Items { get; private set; }

		IDisposable _CollectionDisposer;
		public AppMapContainerViewModel(SelectableAppMapContainerBase container, PageManager pageManager)
			: base (container, pageManager)
		{
			Container = container;
			Items = container.DisplayItems
				.ToReadOnlyReactiveCollection(x => AppMapObjectToViewModel(x));

			_CollectionDisposer = Items.CollectionChangedAsObservable()
				.Subscribe(x =>
				{
					if (x.OldItems != null && x.OldItems.Count > 0)
					{
						foreach (var oldItem in x.OldItems)
						{
							(oldItem as IDisposable)?.Dispose();
						}
					}
				});
		}

		public void Dispose()
		{
			Items?.Dispose();
			_CollectionDisposer?.Dispose();
		}

		private AppMapItemViewModel AppMapObjectToViewModel(IAppMapItem item)
		{
			if (item is SelectableAppMapContainerBase)
			{
				return new AppMapContainerViewModel(item as SelectableAppMapContainerBase, PageManager);
			}
			else 
			{
				return new AppMapItemViewModel(item, PageManager);
			}
		}

		
	}
}
