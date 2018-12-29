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
    public sealed class EpilogueIntroductionPageViewModel : ViewModelBase
    {
        public EpilogueIntroductionPageViewModel(Commands.GoNextIntroductionPageCommand goNext)
        {
            GoNext = goNext;
        }

        public ICommand GoNext { get; }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            GoNext.Execute(null);

            base.OnNavigatedTo(e, viewModelState);
        }
    }
}
