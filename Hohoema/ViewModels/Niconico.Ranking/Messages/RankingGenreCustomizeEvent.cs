using CommunityToolkit.Mvvm.Messaging.Messages;
using NiconicoToolkit.Ranking.Video;

namespace Hohoema.ViewModels.Niconico.Ranking.Messages;

public class RankingGenreCustomizeEventArgs
{
    public RankingGenre RankingGenre { get; set; }
    public string Tag { get; set; }
}

public class RankingGenreFavoriteRequestEventArgs
{
    public RankingGenre RankingGenre { get; set; }
    public string Tag { get; set; }
    public string Label { get; set; }
}


public sealed class RankingGenreShowRequestedEvent : ValueChangedMessage<RankingGenreCustomizeEventArgs>
{
    public RankingGenreShowRequestedEvent(RankingGenreCustomizeEventArgs value) : base(value)
    {
    }
}
public sealed class RankingGenreHiddenRequestedEvent : ValueChangedMessage<RankingGenreCustomizeEventArgs>
{
    public RankingGenreHiddenRequestedEvent(RankingGenreCustomizeEventArgs value) : base(value)
    {
    }
}

public sealed class RankingGenreFavoriteRequestedEvent : ValueChangedMessage<RankingGenreFavoriteRequestEventArgs>
{
    public RankingGenreFavoriteRequestedEvent(RankingGenreFavoriteRequestEventArgs value) : base(value)
    {
    }
}
public sealed class RankingGenreUnFavoriteRequestedEvent : ValueChangedMessage<RankingGenreCustomizeEventArgs>
{
    public RankingGenreUnFavoriteRequestedEvent(RankingGenreCustomizeEventArgs value) : base(value)
    {
    }
}
