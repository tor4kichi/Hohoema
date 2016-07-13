using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	[Flags]
	public enum CommentCommandPermissionType
	{
		Owner     = 0x01,
		User      = 0x02,
		Anonymous = 0x04,
	}
}
