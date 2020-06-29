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
    public sealed class CopyToClipboardCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object content)
        {
            return content != null;
        }

        protected override void Execute(object content)
        {
            if (content is INiconicoObject niconicoContent)
            {
                var uri = ExternalAccessHelper.ConvertToUrl(niconicoContent);
                ClipboardHelper.CopyToClipboard(uri.OriginalString);
            }
            else
            {
                ClipboardHelper.CopyToClipboard(content.ToString());
            }
        }
    }
}
