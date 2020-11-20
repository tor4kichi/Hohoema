using Hohoema.Models.Domain;
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
using Hohoema.Models.Domain.Helpers;
using System.Runtime.InteropServices.WindowsRuntime;
using WinRTXamlToolkit.Async;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Unity;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Windows.UI.Core;
using Hohoema.Presentation.Services;
using Mntone.Nico2;
using System.Reactive.Concurrency;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Events;
using Prism.Unity;
using Hohoema.Presentation.Services.Page;

namespace Hohoema.Presentation.ViewModels
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

        private CancellationTokenSource _navigationCancellationTokenSource;

        protected CancellationToken NavigationCancellationToken => _navigationCancellationTokenSource?.Token ?? default;

        public virtual void Destroy()
        {
            _CompositeDisposable?.Dispose();
        }


        public virtual void OnNavigatingTo(INavigationParameters parameters) 
        {
            Views.PrimaryWindowCoreLayout.SetCurrentNavigationParameters(parameters);
            _navigationCancellationTokenSource = new CancellationTokenSource()
                .AddTo(_NavigatingCompositeDisposable);
        }

        public virtual void OnNavigatedTo(INavigationParameters parameters)
        {
        }

        public virtual void OnNavigatedFrom(INavigationParameters parameters)
        {
            _navigationCancellationTokenSource?.Cancel();
            _NavigatingCompositeDisposable.Dispose();
            _NavigatingCompositeDisposable = new CompositeDisposable();
        }
    }
}
