using Prism.Mvvm;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.Flyouts
{
    public sealed class LiveItemFlyoutViewModel : BindableBase
    {
        public LiveItemFlyoutViewModel(
            Services.ExternalAccessService externalAccessService
            )
        {
            ExternalAccessService = externalAccessService;
        }

        public Services.ExternalAccessService ExternalAccessService { get; }
    }
}
