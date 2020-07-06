using Hohoema.Models.Helpers;
using Hohoema.Models.Repository;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.ExternalAccess.Commands
{
    public sealed class OpenShareUICommand : DelegateCommandBase
    {
        protected override bool CanExecute(object content)
        {
            return Windows.ApplicationModel.DataTransfer.DataTransferManager.IsSupported()
                && (content is INiconicoContent niconicoContent && niconicoContent.Id != null);
        }

        protected override void Execute(object parameter)
        {
            if (parameter is INiconicoContent content)
            {
                var shareContent = ShareHelper.MakeShareText(content);
                ShareHelper.Share(shareContent);
            }
        }
    }
}
