using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using Prism.Events;
using Hohoema.Presentation.ViewModels.Pages.VideoPages;

namespace Hohoema.Presentation.ViewModels.Ranking
{
    public sealed class FavoriteRankingGenreItemCommand : DelegateCommandBase
    {
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
                var ea = App.Current.Container.Resolve<IEventAggregator>();

                if (rankingItem.Genre == null)
                {
                    throw new NotSupportedException();
                }

                ea.GetEvent<Events.RankingGenreFavoriteRequestedEvent>()
                    .Publish(new Events.RankingGenreFavoriteRequestEventArgs()
                    {
                        RankingGenre = rankingItem.Genre.Value,
                        Tag = rankingItem.Tag,
                        Label = rankingItem.Label
                    });

                System.Diagnostics.Debug.WriteLine("FavoriteRankingGenreItemCommand with RankingItem");
            }
        }
    }
}
