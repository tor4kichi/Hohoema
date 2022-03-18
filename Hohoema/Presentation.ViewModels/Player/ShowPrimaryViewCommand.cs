using Hohoema.Models.UseCase.Niconico.Player;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Hohoema.Presentation.ViewModels.Player
{
    public sealed class ShowPrimaryViewCommand : CommandBase
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
}
