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
    public sealed class RemoveFeedSourceCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is ViewModels.FeedSourceBookmark;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is ViewModels.FeedSourceBookmark)
            {
                var vm = parameter as ViewModels.FeedSourceBookmark;
                var feedManager = App.Current.Container.Resolve<FeedManager>();
                feedManager.RemoveFeedSource(vm.Feed, vm.Bookmark);
            }
        }
    }
}
