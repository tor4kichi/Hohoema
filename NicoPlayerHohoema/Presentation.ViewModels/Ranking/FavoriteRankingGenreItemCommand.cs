using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using Prism.Events;

namespace Hohoema.Presentation.ViewModels.Ranking
{
    public sealed class FavoriteRankingGenreItemCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is ViewModels.RankingItem;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is ViewModels.RankingGenreItem)
            {

            }
            else if (parameter is ViewModels.FavoriteRankingGenreGroupItem)
            {

            }
            else if (parameter is ViewModels.RankingItem rankingItem)
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
