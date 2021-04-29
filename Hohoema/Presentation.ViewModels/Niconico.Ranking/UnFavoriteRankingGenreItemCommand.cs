using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using Microsoft.Toolkit.Mvvm.Messaging;
using Hohoema.Presentation.ViewModels.Niconico.Ranking.Messages;

namespace Hohoema.Presentation.ViewModels.Niconico.Ranking
{
    class UnFavoriteRankingGenreItemCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is RankingItem;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is FavoriteRankingGenreGroupItem favGroup)
            {

            }
            if (parameter is RankingItem rankingItem)
            {
                if (rankingItem.Genre == null)
                {
                    throw new NotSupportedException();
                }

                StrongReferenceMessenger.Default.Send(new RankingGenreUnFavoriteRequestedEvent(new()
                {
                    RankingGenre = rankingItem.Genre.Value,
                    Tag = rankingItem.Tag
                }));


                System.Diagnostics.Debug.WriteLine("UnFavoriteRankingGenreItemCommand with RankingItem");
            }
        }
    }
}
