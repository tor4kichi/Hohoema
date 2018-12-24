using NicoPlayerHohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.Commands
{
    public sealed class NicoLiveUserIdAddToNGCommand : DelegateCommandBase
    {
        public NicoLiveUserIdAddToNGCommand(NGSettings ngSettings)
        {
            NgSettings = ngSettings;
        }

        public NGSettings NgSettings { get; }

        protected override bool CanExecute(object parameter)
        {
            return parameter is string;
        }

        protected override void Execute(object parameter)
        {
            var userId = parameter as string;
            var screenName = Database.NicoVideoOwnerDb.Get(userId)?.ScreenName;

            NgSettings.AddNGLiveCommentUserId(userId, screenName);
        }
    }
}
