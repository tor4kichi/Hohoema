using Hohoema.Models.Domain.PageNavigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.PageNavigation
{
	public sealed class LoginRedirectPayload : PagePayloadBase
	{
		public HohoemaPageType RedirectPageType { get; set; }
		public string RedirectParamter { get; set; }
	}

    
}
