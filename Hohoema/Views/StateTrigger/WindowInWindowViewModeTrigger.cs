#nullable enable
using Hohoema.Contracts.Services.Player;
using Hohoema.Services.Player;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Windows.ApplicationModel.Core;

namespace Hohoema.Views.StateTrigger;

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
