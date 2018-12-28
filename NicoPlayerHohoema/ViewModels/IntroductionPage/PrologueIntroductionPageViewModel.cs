using NicoPlayerHohoema.Commands;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class PrologueIntroductionPageViewModel : ViewModelBase
    {
        public PrologueIntroductionPageViewModel(GoNextIntroductionPageCommand goNextIntroduction)
        {
            GoNextIntroduction = goNextIntroduction;
        }

        public GoNextIntroductionPageCommand GoNextIntroduction { get; }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            (GoNextIntroduction as ICommand).Execute(null);

            base.OnNavigatedTo(e, viewModelState);
        }
    }
}
