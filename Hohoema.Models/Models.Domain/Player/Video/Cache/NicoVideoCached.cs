using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Models.Domain.Player.Video.Cache
{
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
            if (obj is NicoVideoCached)
            {
                return Equals(obj as NicoVideoCached);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(NicoVideoCached other)
        {
            return this.VideoId == other.VideoId && this.Quality == other.Quality;
        }




    }
}
