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
    public sealed class EpilogueIntroductionPageViewModel : BindableBase, Prism.Navigation.INavigatedAware
    {
        public EpilogueIntroductionPageViewModel(Commands.GoNextIntroductionPageCommand goNext)
        {
            GoNext = goNext;
        }

        public ICommand GoNext { get; }


        public void OnNavigatedTo(INavigationParameters parameters)
        {
            GoNext.Execute(null);
        }

        public void OnNavigatedFrom(INavigationParameters parameters)
        {
        }
    }
}
