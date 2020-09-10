using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Domain;
using Hohoema.Presentation.Services.Page;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Windows.Foundation;

namespace Hohoema.Presentation.ViewModels
{
    public class SplashPageViewModel : BindableBase
    {
        public SplashPageViewModel(
            INavigationService ns,
            PageManager pageManager
            )
        {
            PageManager = pageManager;
            NavigationService = ns;
        }


        PageManager PageManager { get; }
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
