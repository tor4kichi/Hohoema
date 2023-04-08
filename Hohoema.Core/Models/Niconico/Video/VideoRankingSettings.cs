using Hohoema.Infra;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Ranking.Video;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hohoema.Models.Niconico.Video;

public class VideoRankingSettings : FlagsRepositoryBase
{
    [System.Obsolete]
    public VideoRankingSettings()
    {
        _HiddenTags = Read(new List<RankingGenreTag>(), nameof(HiddenTags));
        HiddenTags = new ReadOnlyCollection<RankingGenreTag>(_HiddenTags);
        HiddenGenres = Read(new HashSet<RankingGenre>(), nameof(HiddenGenres));
        _FavoriteTags = Read(new List<RankingGenreTag>(), nameof(FavoriteTags));
        FavoriteTags = new ReadOnlyCollection<RankingGenreTag>(_FavoriteTags);
    }

    private readonly List<RankingGenreTag> _HiddenTags;
    public IReadOnlyCollection<RankingGenreTag> HiddenTags { get; }

    public HashSet<RankingGenre> HiddenGenres { get; }

    private readonly List<RankingGenreTag> _FavoriteTags;
    public IReadOnlyCollection<RankingGenreTag> FavoriteTags { get; }


    public bool IsHiddenTag(RankingGenre genre, string tag)
    {
        return _HiddenTags.Any(x => x.Genre == genre && x.Tag == tag);
    }

    [System.Obsolete]
    public void RemoveHiddenTag(RankingGenre genre, string tag)
    {
        RankingGenreTag target = _HiddenTags.FirstOrDefault(x => x.Genre == genre && x.Tag == tag);
        if (target != null)
        {
            _ = _HiddenTags.Remove(target);
            Save(_HiddenTags.ToList(), nameof(HiddenTags));
        }
    }

    [System.Obsolete]
    public void AddHiddenTag(RankingGenre genre, string tag, string label)
    {
        if (false == _HiddenTags.Any(x => x.Genre == genre && x.Tag == tag))
        {
            _HiddenTags.Add(new RankingGenreTag()
            {
                Tag = tag,
                Label = label,
                Genre = genre
            });
            Save(_HiddenTags.ToList(), nameof(HiddenTags));
        }
    }


    public bool IsHiddenGenre(RankingGenre genre)
    {
        return HiddenGenres.Contains(genre);
    }

    [System.Obsolete]
    public void RemoveHiddenGenre(RankingGenre genre)
    {
        _ = HiddenGenres.Remove(genre);
        Save(HiddenGenres, nameof(HiddenGenres));
    }

    [System.Obsolete]
    public void ResetHiddenGenre(IEnumerable<RankingGenre> genreList)
    {
        HiddenGenres.Clear();
        foreach (RankingGenre genre in genreList) { _ = HiddenGenres.Add(genre); }
        Save(HiddenGenres, nameof(HiddenGenres));
    }

    [System.Obsolete]
    public void AddHiddenGenre(RankingGenre genre)
    {
        if (false == HiddenGenres.Contains(genre))
        {
            _ = HiddenGenres.Add(genre);
            Save(HiddenGenres, nameof(HiddenGenres));
        }
    }


    public bool IsFavoriteTag(RankingGenre genre, string tag)
    {
        return FavoriteTags.Any(x => x.Genre == genre && x.Tag == tag);
    }

    [System.Obsolete]
    public void RemoveFavoriteTag(RankingGenre genre, string tag)
    {
        RankingGenreTag target = FavoriteTags.FirstOrDefault(x => x.Genre == genre && x.Tag == tag);
        if (target != null)
        {
            _ = _FavoriteTags.Remove(target);
            Save(_FavoriteTags.ToList(), nameof(FavoriteTags));
        }
    }

    [System.Obsolete]
    public void AddFavoriteTag(RankingGenre genre, string tag, string label)
    {
        if (false == FavoriteTags.Any(x => x.Genre == genre && x.Tag == tag))
        {
            _FavoriteTags.Add(new RankingGenreTag()
            {
                Tag = tag,
                Label = label,
                Genre = genre
            });
            Save(_FavoriteTags.ToList(), nameof(FavoriteTags));
        }
    }
}

public record RankingGenreTag
{
    public string Label { get; set; }
    public RankingGenre Genre { get; set; }
    public string Tag { get; set; }
}
