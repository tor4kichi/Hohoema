using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PlayerSidePaneContent
{
	public abstract class SidePaneContentViewModelBase : BindableBase, IDisposable
	{
        private SynchronizationContextScheduler _CurrentWindowContextScheduler;
        public SynchronizationContextScheduler CurrentWindowContextScheduler
        {
            get
            {
                return _CurrentWindowContextScheduler
                    ?? (_CurrentWindowContextScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current));
            }
        }

        protected CompositeDisposable _CompositeDisposable;

		public SidePaneContentViewModelBase()
		{
			_CompositeDisposable = new CompositeDisposable();
		}

        virtual public Task OnEnter() { return Task.CompletedTask; }
        virtual public void OnLeave() { }


        protected virtual void OnDispose() { }

		public void Dispose()
		{
			OnDispose();

			_CompositeDisposable?.Dispose();
		}
	}
}
