using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.ViewModels.Niconico.Ranking.Messages;

namespace Hohoema.ViewModels.Niconico.Ranking
{
    public sealed class HiddenRankingGenreItemCommand : CommandBase
    {

        public HiddenRankingGenreItemCommand()
        {
        }

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

                WeakReferenceMessenger.Default.Send(new RankingGenreHiddenRequestedEvent(new()
                {
                    RankingGenre = rankingItem.Genre.Value,
                    Tag = rankingItem.Tag
                }));

                System.Diagnostics.Debug.WriteLine("HiddenRankingGenreItemCommand with RankingItem");
            }
        }
    }
}
