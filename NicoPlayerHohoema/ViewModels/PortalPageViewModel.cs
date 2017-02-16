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
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;

namespace NicoPlayerHohoema.ViewModels
{
	public class PortalPageViewModel : HohoemaViewModelBase
	{
        public AppMapManager AppMapManager { get; private set; }
        public AppMapContainerViewModel Root { get; private set; }

		public PortalPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, AppMapManager appMapManager)
			: base(hohoemaApp, pageManager)
		{
            AppMapManager = appMapManager;
            Root = new AppMapContainerViewModel(AppMapManager.Root, PageManager);
		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            await AppMapManager.Refresh();

            while (AppMapManager.NowRefreshing)
            {
                await Task.Delay(50);
            }

            await base.NavigatedToAsync(cancelToken, e, viewModelState);
        }




        private DelegateCommand _RefreshCommand;
        public DelegateCommand RefreshCommand
        {
            get
            {
                return _RefreshCommand
                    ?? (_RefreshCommand = new DelegateCommand(async () =>
                    {
                        await AppMapManager.Refresh();
                    }));
            }
        }


        internal static AppMapItemViewModel AppMapObjectToViewModel(IAppMapItem item, PageManager pageManager)
		{
            Debug.WriteLine(item.PrimaryLabel);

            if (item is IAppMapContainer)
			{
				return new AppMapContainerViewModel(item as IAppMapContainer, pageManager);
			}
			else
			{
				return new AppMapItemViewModel(item, pageManager);
			}
		}


	}



	public class AppMapItemViewModel
	{
		public IAppMapItem Item { get; private set; }

		public bool IsHiddenLabel { get; private set; }
		public bool IsHiddenSecondaryLabel { get; private set; }



		public PageManager PageManager { get; private set; }

		public AppMapItemViewModel(IAppMapItem item, PageManager pageManager)
		{
			Item = item;
			PageManager = pageManager;

//			IsHiddenLabel = Item.PageType == HohoemaPageType.Portal;
			IsHiddenSecondaryLabel = String.IsNullOrWhiteSpace(Item.SecondaryLabel);
		}


		private DelegateCommand _OpenItemPageCommand;
		public DelegateCommand OpenItemPageCommand
		{
			get
			{
				return _OpenItemPageCommand
					?? (_OpenItemPageCommand = new DelegateCommand(() => 
					{
                        Item.SelectedAction();

//                        PageManager.OpenPage(Item.PageType, Item.Parameter);
					}));
			}
		}

		
	}


	public class AppMapContainerViewModel : AppMapItemViewModel, IDisposable
	{
		public IAppMapContainer OriginalContainer { get; set; }

		public ReadOnlyReactiveCollection<AppMapItemViewModel> Items { get; private set; }

		IDisposable _CollectionDisposer;


		public int ItemWidth { get; private set; }
		public int ItemHeight { get; private set; }

		public AppMapContainerViewModel(IAppMapContainer container, PageManager pageManager)
			: base(container, pageManager)
		{
			OriginalContainer = container;

            Items = container.DisplayItems
				.ToReadOnlyReactiveCollection(x => PortalPageViewModel.AppMapObjectToViewModel(x, pageManager));

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

			switch (OriginalContainer.ItemDisplayType)
			{
				case ContainerItemDisplayType.Normal:
					ItemWidth = 200;
					ItemHeight = 48;
					break;
				case ContainerItemDisplayType.Card:
					ItemWidth = 80;
					ItemHeight = 80;
					break;
				case ContainerItemDisplayType.TwoLineText:
					ItemWidth = 392;
					ItemHeight = 56;
					break;
				default:
					break;
			}
		}

        public void Dispose()
		{
			Items?.Dispose();
			_CollectionDisposer?.Dispose();
		}

        private DelegateCommand _RefreshCommand;
        public DelegateCommand RefreshCommand
        {
            get
            {
                return _RefreshCommand
                    ?? (_RefreshCommand = new DelegateCommand(async () =>
                    {
                        await OriginalContainer.Refresh();
                    }));
            }
        }


    }


	



}
