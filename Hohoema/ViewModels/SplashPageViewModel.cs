using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Windows.Foundation;

namespace Hohoema.ViewModels
{
    public class SplashPageViewModel : BindableBase
    {
        public SplashPageViewModel(
            INavigationService ns,
            Services.PageManager pageManager
            )
        {
            PageManager = pageManager;
            NavigationService = ns;
        }


        Services.PageManager PageManager { get; }
        INavigationService NavigationService { get; }



        private DelegateCommand _OpenLoginPageCommand;
        public DelegateCommand OpenLoginPageCommand
        {
            get
            {
                return _OpenLoginPageCommand
                    ?? (_OpenLoginPageCommand = new DelegateCommand(() => 
                    {
                        // PageManager.OpenPage(HohoemaPageType.Login);
                    }));
            }
        }



    }
}
