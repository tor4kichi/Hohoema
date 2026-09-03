#nullable enable
using Hohoema.Infra;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Ranking.Video;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hohoema.Models.Niconico.Video;

public class VideoRankingSettings : FlagsRepositoryBase
{
    public VideoRankingSettings()
    {

    }


    public void SetHiddenRankingGenreIds(List<string> genreIds)
    {
        Save(genreIds, "HiddenRankingGenreId");
    }

    public List<string> GetHiddenRankingGenreIds()
    {
        return Read<List<string>>([], "HiddenRankingGenreId");
    }
}
