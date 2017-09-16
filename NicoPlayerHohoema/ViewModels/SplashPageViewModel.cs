using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Windows.Foundation;

namespace NicoPlayerHohoema.ViewModels
{
    public class SplashPageViewModel : ViewModelBase
    {
        private DelegateCommand _OpenLoginPageCommand;
        public DelegateCommand OpenLoginPageCommand
        {
            get
            {
                return _OpenLoginPageCommand
                    ?? (_OpenLoginPageCommand = new DelegateCommand(() => 
                    {
                        PageManager.OpenPage(HohoemaPageType.Login);
                    }));
            }
        }

        PageManager PageManager { get; }

        INavigationService NavigationService { get; }

        public SplashPageViewModel(PageManager pageManager, INavigationService ns)
        {
            PageManager = pageManager;
            NavigationService = ns;
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            base.OnNavigatingFrom(e, viewModelState, suspending);
        }
    }
}
