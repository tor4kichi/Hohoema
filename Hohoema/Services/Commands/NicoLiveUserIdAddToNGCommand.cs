using Hohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace Hohoema.Commands
{
    public sealed class NicoLiveUserIdAddToNGCommand : DelegateCommandBase
    {
        private readonly PlayerSettings _playerSettings;

        public NicoLiveUserIdAddToNGCommand(PlayerSettings playerSettings)
        {
            _playerSettings = playerSettings;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is string;
        }

        protected override void Execute(object parameter)
        {
            var userId = parameter as string;
            var screenName = Database.NicoVideoOwnerDb.Get(userId)?.ScreenName;

            _playerSettings.AddNGLiveCommentUserId(userId, screenName);
        }
    }
}
