using NiconicoToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Video
{
	public class NicoVideoTag : ITag
	{
		public string Tag { get; internal set; }
		public bool IsCategoryTag { get; internal set; }
		public bool IsLocked { get; internal set; }
		public bool IsDictionaryExists { get; internal set; }

		string ITag.Tag => Tag;

        string Label => Tag;

        public NicoVideoTag() { }

		public NicoVideoTag(string tag)
		{
			Tag = tag;
			IsCategoryTag = false;
			IsLocked = false;
		}
	}
}
