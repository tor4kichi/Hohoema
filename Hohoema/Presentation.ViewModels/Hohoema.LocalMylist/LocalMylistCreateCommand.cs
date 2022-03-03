using Hohoema.Models.Domain.LocalMylist;
using Hohoema.Models.UseCase.Hohoema.LocalMylist;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Hohoema.LocalMylist
{
    public sealed class LocalMylistCreateCommand : CommandBase
    {
        private readonly LocalMylistManager _localMylistManager;

        public LocalMylistCreateCommand(LocalMylistManager localMylistManager)
        {
            _localMylistManager = localMylistManager;
        }

        protected override bool CanExecute(object parameter)
        {
            if (parameter is string p)
            {
                return !string.IsNullOrWhiteSpace(p);
            }
            else
            {
                return false;
            }
        }

        protected override void Execute(object parameter)
        {
            if (parameter is string label)
            {
                _localMylistManager.CreatePlaylist(label);
            }
        }
    }
}
