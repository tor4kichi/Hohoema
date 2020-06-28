﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services.Page
{
	public sealed class LoginRedirectPayload : PagePayloadBase
	{
		public HohoemaPageType RedirectPageType { get; set; }
		public string RedirectParamter { get; set; }
	}

    
}
