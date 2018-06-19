using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class EpilogueIntroductionPageViewModel : ViewModelBase
    {
        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            var goNextCommand = new Commands.GoNextIntroductionPageCommand() as System.Windows.Input.ICommand;
            if (goNextCommand != null)
            {
                goNextCommand.Execute(null);
            }

            base.OnNavigatedTo(e, viewModelState);
        }
    }
}
