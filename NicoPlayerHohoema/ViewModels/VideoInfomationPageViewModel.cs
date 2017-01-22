using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;

namespace NicoPlayerHohoema.ViewModels
{
    public class VideoInfomationPageViewModel : HohoemaViewModelBase
    {
        public VideoInfomationPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
            : base(hohoemaApp, pageManager, canActivateBackgroundUpdate:true)
        {
            ChangeRequireServiceLevel(HohoemaAppServiceLevel.OnlineWithoutLoggedIn);
        }

        
    }
}
