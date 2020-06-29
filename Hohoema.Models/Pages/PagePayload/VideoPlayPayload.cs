using Mntone.Nico2;
using Hohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Niconico.Video;

namespace Hohoema.Models.Pages.PagePayload
{
	public class VideoPlayPayload : PagePayloadBase<VideoPlayPayload>
	{
		public string VideoId { get; set; }
		public NicoVideoQuality? Quality { get; set; }
	}
}
