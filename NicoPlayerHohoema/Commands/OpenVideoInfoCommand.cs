using Prism.Commands;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Commands
{
    public sealed class OpenVideoInfoCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;

                var pageManager = HohoemaCommnadHelper.GetPageManager();
                pageManager.OpenPage(Models.HohoemaPageType.VideoInfomation, content.Id);
            }
        }
    }
}
