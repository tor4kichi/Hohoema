using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Disposables;
using System.Threading;
using Reactive.Bindings.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Views.Pages;
using Hohoema.Contracts.Services.Navigations;
using Hohoema.Contracts.Services.Navigations;

namespace Hohoema.ViewModels
{
	public abstract class HohoemaPageViewModelBase : NavigationAwareViewModelBase, IDisposable
	{
        public HohoemaPageViewModelBase()
        {
            _CompositeDisposable = new CompositeDisposable();
        }
        
        protected CompositeDisposable _CompositeDisposable { get; private set; }
        protected CompositeDisposable _navigationDisposables { get; private set; }

        private CancellationTokenSource _navigationCts;

        protected CancellationToken NavigationCancellationToken { get; private set; }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }


        public virtual void Dispose()
        {
            _CompositeDisposable?.Dispose();
        }

        public override void OnNavigatingTo(INavigationParameters parameters) 
        {
            _navigationDisposables?.Dispose();
            _navigationDisposables = new();
            _navigationCts = new CancellationTokenSource()
                .AddTo(_navigationDisposables);
            NavigationCancellationToken = _navigationCts.Token;
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            _navigationCts?.Cancel();
            _navigationCts?.Dispose();
            _navigationDisposables?.Dispose();
            _navigationDisposables = new();
        }

    }
}
