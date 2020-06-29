using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Live
{
	public enum LiveStatusType
	{
        Unknown,
        NotFound,
		ComingSoon,
        OnAir,
        Closed,
        Maintenance,
		CommunityMemberOnly,
		Full,
		PremiumOnly,
		NotLogin,
        ServiceTemporarilyUnavailable,
    }
}
