using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Disposables;
using System.Threading;
using Reactive.Bindings.Extensions;
using Prism.Mvvm;
using Prism.Navigation;
using Hohoema.Presentation.Views.Pages;
using Uno.Threading;

namespace Hohoema.Presentation.ViewModels
{
	public abstract class HohoemaViewModelBase : BindableBase, INavigationAware, IDestructible, IDisposable
	{
        public HohoemaViewModelBase()
        {
            _CompositeDisposable = new CompositeDisposable();
            _NavigatingCompositeDisposable = new CompositeDisposable();
        }
        
        protected CompositeDisposable _CompositeDisposable { get; private set; }
        protected CompositeDisposable _NavigatingCompositeDisposable { get; private set; }

        private CancellationTokenSource _navigationCancellationTokenSource;

        protected CancellationToken NavigationCancellationToken { get; private set; }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public virtual void Destroy()
        {
            _CompositeDisposable?.Dispose();
        }



        void IDisposable.Dispose()
        {
            _CompositeDisposable?.Dispose();
        }

        public virtual void OnNavigatingTo(INavigationParameters parameters) 
        {
            Views.Pages.PrimaryWindowCoreLayout.SetCurrentNavigationParameters(parameters);
            _navigationCancellationTokenSource = new CancellationTokenSource()
                .AddTo(_NavigatingCompositeDisposable);
            NavigationCancellationToken = _navigationCancellationTokenSource.Token;
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
