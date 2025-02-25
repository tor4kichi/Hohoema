#nullable enable
using System;
using System.Reactive.Concurrency;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Hohoema.ViewModels.Player;

public sealed partial class ShowPrimaryViewCommand : CommandBase
{
    private readonly IScheduler _scheduler;

    public ShowPrimaryViewCommand(IScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override void Execute(object parameter)
    {
        _scheduler.Schedule(async () => 
        {
            var mainViewId = ApplicationView.GetApplicationViewIdForWindow(Window.Current.CoreWindow);
            await ApplicationViewSwitcher.TryShowAsStandaloneAsync(mainViewId);
        });
    }
}
