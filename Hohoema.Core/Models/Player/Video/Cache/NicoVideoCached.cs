using System;
using Windows.Storage;

namespace Hohoema.Models.Player.Video.Cache;

public class NicoVideoCached : IEquatable<NicoVideoCached>
{
    public string VideoId { get; set; }
    public NicoVideoQuality_Legacy Quality { get; set; }
    public DateTime RequestAt { get; set; } = DateTime.Now;

    public IStorageFile File { get; set; }

    public NicoVideoCached(string videoId, NicoVideoQuality_Legacy quality, DateTime requestAt, IStorageFile file)
    {
        VideoId = videoId;
        Quality = quality;
        RequestAt = requestAt;
        File = file;
    }

    public override int GetHashCode()
    {
        return VideoId.GetHashCode() ^ Quality.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is NicoVideoCached && Equals(obj as NicoVideoCached);
    }

    public bool Equals(NicoVideoCached other)
    {
        return VideoId == other.VideoId && Quality == other.Quality;
    }




}
