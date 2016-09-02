using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.UserInfo
{
	public class UserInfo
	{
		public uint UserId { get; private set; }
		public string Name { get; private set; }
		public Uri IconUri { get; private set; }
	}
}
