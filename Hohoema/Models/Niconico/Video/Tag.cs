using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Video
{
	public class NicoVideoTag : Interfaces.ITag
	{
		public string Tag { get; private set; }
		public bool IsCategoryTag { get; internal set; }
		public bool IsLocked { get; internal set; }

		public NicoVideoTag(string tag)
		{
			Tag = tag;
			IsCategoryTag = false;
			IsLocked = false;
		}
	}
}
