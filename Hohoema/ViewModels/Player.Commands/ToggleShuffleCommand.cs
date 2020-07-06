using Hohoema.Models.Repository.App;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Player.Commands
{
    public sealed class ToggleShuffleCommand : DelegateCommandBase
    {
        private readonly PlayerSettingsRepository _playerSettingsRepository;

        public ToggleShuffleCommand(PlayerSettingsRepository playerSettingsRepository)
        {
            _playerSettingsRepository = playerSettingsRepository;
        }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            _playerSettingsRepository.IsShuffleEnable = !_playerSettingsRepository.IsShuffleEnable;
        }
    }
}
