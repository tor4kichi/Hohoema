using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using Hohoema.Presentation.ViewModels.Pages.VideoPages;

namespace Hohoema.Presentation.ViewModels.Ranking
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
                var ea = App.Current.Container.Resolve<IEventAggregator>();

                if (rankingItem.Genre == null)
                {
                    throw new NotSupportedException();
                }

                ea.GetEvent<Events.RankingGenreUnFavoriteRequestedEvent>()
                    .Publish(new Events.RankingGenreCustomizeEventArgs()
                    {
                        RankingGenre = rankingItem.Genre.Value,
                        Tag = rankingItem.Tag
                    });

                System.Diagnostics.Debug.WriteLine("UnFavoriteRankingGenreItemCommand with RankingItem");
            }
        }
    }
}
