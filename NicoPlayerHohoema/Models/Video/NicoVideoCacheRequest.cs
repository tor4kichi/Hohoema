using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class NicoVideoCacheRequest : IEquatable<NicoVideoCacheRequest>
	{
		public string RawVideoid { get; set; }
		public NicoVideoQuality Quality { get; set; }

		public override int GetHashCode()
		{
			return RawVideoid.GetHashCode() ^ Quality.GetHashCode();
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
			return this.RawVideoid == other.RawVideoid && this.Quality == other.Quality;
		}
	}
}
