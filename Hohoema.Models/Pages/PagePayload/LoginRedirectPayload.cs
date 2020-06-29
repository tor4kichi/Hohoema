using Hohoema.Models.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Pages.PagePayload
{
	public sealed class LoginRedirectPayload : PagePayloadBase<LoginRedirectPayload>
	{
		public HohoemaPageType RedirectPageType { get; set; }
		public string RedirectParamter { get; set; }
	}

    
}
