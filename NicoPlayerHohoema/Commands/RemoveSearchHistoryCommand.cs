using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Commands
{
    public sealed class RemoveSearchHistoryCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Database.SearchHistory;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Database.SearchHistory)
            {
                var history = parameter as Database.SearchHistory;
                Database.SearchHistoryDb.Remove(history.Keyword, history.Target);
            }
        }
    }
}
