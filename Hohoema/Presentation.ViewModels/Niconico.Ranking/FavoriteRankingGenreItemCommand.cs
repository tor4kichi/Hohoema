using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.Messaging;
using Hohoema.Presentation.ViewModels.Pages.Niconico;
using Hohoema.Presentation.ViewModels.Niconico.Ranking.Messages;

namespace Hohoema.Presentation.ViewModels.Niconico.Ranking
{
    public sealed class FavoriteRankingGenreItemCommand : CommandBase
    {

        public FavoriteRankingGenreItemCommand()
        {
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is RankingItem;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is RankingGenreItem)
            {

            }
            else if (parameter is FavoriteRankingGenreGroupItem)
            {

            }
            else if (parameter is RankingItem rankingItem)
            {
                if (rankingItem.Genre == null)
                {
                    throw new NotSupportedException();
                }

                WeakReferenceMessenger.Default.Send(new RankingGenreFavoriteRequestedEvent(new ()
                {
                    RankingGenre = rankingItem.Genre.Value,
                    Tag = rankingItem.Tag,
                    Label = rankingItem.Label
                }));

                System.Diagnostics.Debug.WriteLine("FavoriteRankingGenreItemCommand with RankingItem");
            }
        }
    }
}
