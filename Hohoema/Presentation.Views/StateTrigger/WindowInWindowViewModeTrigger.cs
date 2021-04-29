using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.UseCase.Player;
using Prism.Ioc;
using Reactive.Bindings.Extensions;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Hohoema.Presentation.Views.StateTrigger
{
    public sealed class WindowInWindowViewModeTrigger : InvertibleStateTrigger, IDisposable
    {
        private PrimaryViewPlayerManager _primaryViewPlayerManager;

        IDisposable _disposable;
        public WindowInWindowViewModeTrigger()
        {
            var scheduler = App.Current.Container.Resolve<IScheduler>();
            var coreApplication = CoreApplication.GetCurrentView();
            if (coreApplication.IsMain)
            {
                _primaryViewPlayerManager = App.Current.Container.Resolve<PrimaryViewPlayerManager>();
                _disposable = _primaryViewPlayerManager.ObserveProperty(x => x.DisplayMode)
                    .ObserveOn(scheduler)
                    .Subscribe(mode =>
                    {
                        SetActiveInvertible(mode == PrimaryPlayerDisplayMode.WindowInWindow);
                    });
            }
            else
            {
                SetActiveInvertible(false);
            }
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}
