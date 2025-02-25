﻿#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using NiconicoToolkit.Ranking.Video;

namespace Hohoema.ViewModels.Niconico.Ranking;

public class RankingItem : ObservableObject
{
    public string Label { get; set; }

    public RankingGenre? Genre { get; set; }
    public string Tag { get; set; }

    private bool _IsFavorite;
    public bool IsFavorite
    {
        get { return _IsFavorite; }
        set { SetProperty(ref _IsFavorite, value); }
    }
}
