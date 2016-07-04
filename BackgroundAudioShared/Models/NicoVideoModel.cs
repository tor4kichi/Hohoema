using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BackgroundAudioShared
{
	[DataContract]
	public class NicoVideoModel
	{
		[DataMember]
		public string Title { get; set; }
		[DataMember]
		public string VideoId { get; set; }
		[DataMember]
		public VideoQuality Quality { get; set; }
		[DataMember]
		public Uri ThumbnailUri { get; set; }
	}
}
