using Prism.Mvvm;
using Prism.Navigation;
using System.Reactive.Disposables;

namespace Hohoema.ViewModels
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


        public virtual void OnNavigatingTo(INavigationParameters parameters) { }

        public virtual void OnNavigatedTo(INavigationParameters parameters)
        {
        }

        public virtual void OnNavigatedFrom(INavigationParameters parameters)
        {
            _NavigatingCompositeDisposable.Dispose();
            _NavigatingCompositeDisposable = new CompositeDisposable();
        }
    }
}
