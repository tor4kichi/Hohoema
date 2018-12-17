using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class VideoItemFlyoutViewModel : ViewModelBase
    {
        public VideoItemFlyoutViewModel(
            Models.UserMylistManager userMylistManager
            )
        {
            UserMylistManager = userMylistManager;
        }

        public Models.UserMylistManager UserMylistManager { get; }
    }
}
