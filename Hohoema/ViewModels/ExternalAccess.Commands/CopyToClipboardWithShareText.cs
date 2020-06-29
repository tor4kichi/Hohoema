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
    public sealed class CopyToClipboardWithShareText : DelegateCommandBase
    {
        protected override bool CanExecute(object content)
        {
            return content != null;
        }

        protected override void Execute(object content)
        {
            if (content is INiconicoContent niconicoContent)
            {
                var shareContent = ShareHelper.MakeShareText(niconicoContent);
                ClipboardHelper.CopyToClipboard(shareContent);
            }
            else if (content is string contentId)
            {
                var video = Database.NicoVideoDb.Get(contentId);
                if (video != null)
                {
                    var shareContent = ShareHelper.MakeShareText(video);
                    ClipboardHelper.CopyToClipboard(shareContent);
                }
            }
            else
            {
                ClipboardHelper.CopyToClipboard(content.ToString());
            }
        }
    }
}
