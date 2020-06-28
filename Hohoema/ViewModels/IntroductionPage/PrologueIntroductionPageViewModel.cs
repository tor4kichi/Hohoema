using Hohoema.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hohoema.ViewModels
{
    public sealed class PrologueIntroductionPageViewModel : BindableBase, Prism.Navigation.INavigatedAware
    {
        public PrologueIntroductionPageViewModel(GoNextIntroductionPageCommand goNextIntroduction)
        {
            GoNextIntroduction = goNextIntroduction;
        }

        public GoNextIntroductionPageCommand GoNextIntroduction { get; }

        public void OnNavigatedFrom(INavigationParameters parameters)
        {
            
        }

        public void OnNavigatedTo(INavigationParameters parameters)
        {
            (GoNextIntroduction as ICommand).Execute(null);
        }
    }
}
