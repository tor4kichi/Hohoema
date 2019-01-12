using NicoPlayerHohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Disposables;
using System.Threading;
using Windows.UI.Xaml;
using Windows.Foundation;
using NicoPlayerHohoema.Models.Helpers;
using System.Runtime.InteropServices.WindowsRuntime;
using WinRTXamlToolkit.Async;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Unity;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Windows.UI.Core;
using NicoPlayerHohoema.Services;
using Mntone.Nico2;
using System.Reactive.Concurrency;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Events;
using Prism.Unity;
using NicoPlayerHohoema.Services.Page;

namespace NicoPlayerHohoema.ViewModels
{
	public abstract class HohoemaViewModelBase : BindableBase, INavigationAware, IDestructible
	{
        public HohoemaViewModelBase()
        {
            _CompositeDisposable = new CompositeDisposable();
            _NavigatingCompositeDisposable = new CompositeDisposable();
        }
        
        protected CompositeDisposable _CompositeDisposable { get; private set; }
        protected CompositeDisposable _NavigatingCompositeDisposable { get; private set; }

        public virtual void Destroy()
        {
            _CompositeDisposable?.Dispose();
        }

        
        public virtual void OnNavigatingTo(INavigationParameters parameters)
        {
            _NavigatingCompositeDisposable = new CompositeDisposable();
        }

        public virtual void OnNavigatedTo(INavigationParameters parameters)
        {
            var ea = App.Current.Container.Resolve<IEventAggregator>();
            ea.GetEvent<PinningCurrentPageRequestEvent>()
                .Subscribe(() =>
                {
                    if (TryGetHohoemaPin(out var pin))
                    {
                        var pinSettings = App.Current.Container.Resolve<PinSettings>();
                        if (!pinSettings.Pins.Any(x => x.PageType == pin.PageType && x.Parameter == pin.Parameter))
                        {
                            pinSettings.Pins.Add(pin);
                        }
                    }
                })
                .AddTo(_NavigatingCompositeDisposable);
        }

        public virtual void OnNavigatedFrom(INavigationParameters parameters)
        {
            _NavigatingCompositeDisposable.Dispose();
            _NavigatingCompositeDisposable = null;
        }


        protected abstract bool TryGetHohoemaPin(out HohoemaPin pin);
    }
}
