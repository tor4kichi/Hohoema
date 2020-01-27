using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace NicoPlayerHohoema.Models.Cache
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
}
