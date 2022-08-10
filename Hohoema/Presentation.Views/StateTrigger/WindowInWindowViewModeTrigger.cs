using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.UseCase.Niconico.Player;
using Reactive.Bindings.Extensions;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Hohoema.Presentation.Views.StateTrigger
{
    public sealed class WindowInWindowViewModeTrigger : InvertibleStateTrigger, IDisposable, ITriggerValue
    {
        private PrimaryViewPlayerManager _primaryViewPlayerManager;

        IDisposable _disposable;
        public WindowInWindowViewModeTrigger()
        {
            var scheduler = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<IScheduler>();
            var coreApplication = CoreApplication.GetCurrentView();
            if (coreApplication.IsMain)
            {
                _primaryViewPlayerManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<PrimaryViewPlayerManager>();
                _disposable = _primaryViewPlayerManager.ObserveProperty(x => x.DisplayMode)
                    .ObserveOn(scheduler)
                    .Subscribe(mode =>
                    {
                        SetActiveInvertible(mode == PlayerDisplayMode.WindowInWindow);
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
