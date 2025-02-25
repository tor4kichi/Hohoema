#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.ViewModels.Niconico.Ranking.Messages;
using System;

namespace Hohoema.ViewModels.Niconico.Ranking;

public sealed partial class HiddenRankingGenreItemCommand : CommandBase
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
