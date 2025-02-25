#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.ViewModels.Niconico.Ranking.Messages;
using System;

namespace Hohoema.ViewModels.Niconico.Ranking;

public sealed partial class FavoriteRankingGenreItemCommand : CommandBase
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
