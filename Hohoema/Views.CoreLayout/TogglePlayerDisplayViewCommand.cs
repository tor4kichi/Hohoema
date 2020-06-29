using Hohoema.Models;
using Prism.Commands;
using Prism.Events;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services.Player
{
    public sealed class TogglePlayerDisplayViewCommand : DelegateCommandBase, IDisposable
    {
        private readonly PlayerSettings _playerSettings;
        private readonly IEventAggregator _eventAggregator;

        public TogglePlayerDisplayViewCommand(
            PlayerSettings playerSettings,
            IEventAggregator eventAggregator
            )
        {
            _playerSettings = playerSettings;
            _eventAggregator = eventAggregator;
        }

        public void Dispose()
        {
        }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            _eventAggregator.GetEvent<ChangePlayerDisplayViewRequestEvent>()
                .Publish();
        }
    }
}
