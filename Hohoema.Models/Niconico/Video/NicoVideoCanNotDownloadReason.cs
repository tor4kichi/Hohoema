using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models
{
	public enum NicoVideoCanNotDownloadReason
	{
		Unknown,
		Offline,
		NotExist,
		OnlyLowQualityWithoutPremiumUser,
	}
}
