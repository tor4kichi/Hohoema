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
            return parameter is Models.Db.SearchHistory;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Models.Db.SearchHistory)
            {
                var history = parameter as Models.Db.SearchHistory;
                Models.Db.SearchHistoryDb.RemoveHistory(history.Keyword, history.Target);
            }
        }
    }
}
