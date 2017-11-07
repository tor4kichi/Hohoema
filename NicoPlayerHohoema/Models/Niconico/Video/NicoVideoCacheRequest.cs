using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
	public class NicoVideoCacheRequest : IEquatable<NicoVideoCacheRequest>
	{
		public string RawVideoId { get; set; }
		public NicoVideoQuality Quality { get; set; }
        public DateTime RequestAt { get; set; } = DateTime.Now;
        public bool IsRequireForceUpdate { get; set; } = false;

        public override int GetHashCode()
		{
			return RawVideoId.GetHashCode() ^ Quality.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is NicoVideoCacheRequest)
			{
				return Equals(obj as NicoVideoCacheRequest);
			}
			else
			{
				return false;
			}
		}

		public bool Equals(NicoVideoCacheRequest other)
		{
			return this.RawVideoId == other.RawVideoId && this.Quality == other.Quality;
		}
	}

    public class NicoVideoCacheInfo : NicoVideoCacheRequest, IEquatable<NicoVideoCacheInfo>
    {
        public string FilePath { get; set; }

        public NicoVideoCacheInfo() { }

        public NicoVideoCacheInfo(NicoVideoCacheRequest req, string filePath)
        {
            RawVideoId = req.RawVideoId;
            Quality = req.Quality;
            IsRequireForceUpdate = req.IsRequireForceUpdate;
            RequestAt = req.RequestAt;
            FilePath = filePath;
        }



        public override int GetHashCode()
        {
            return RawVideoId.GetHashCode() ^ Quality.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is NicoVideoCacheRequest)
            {
                return Equals(obj as NicoVideoCacheRequest);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(NicoVideoCacheInfo other)
        {
            return this.RawVideoId == other.RawVideoId && this.Quality == other.Quality;
        }

        internal async Task<bool> Delete()
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(FilePath);
                await file.DeleteAsync(option: StorageDeleteOption.PermanentDelete);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }



    public static class NicoVideoCacheRequestExtension
    {
        public static NicoVideoCacheState ToCacheState(this NicoVideoCacheRequest req)
        {
            if (req is NicoVideoCacheInfo) { return NicoVideoCacheState.Cached; }
            else if (req is NicoVideoCacheProgress) { return NicoVideoCacheState.Downloading; }
            else { return NicoVideoCacheState.Pending; }
        }
    }
}
