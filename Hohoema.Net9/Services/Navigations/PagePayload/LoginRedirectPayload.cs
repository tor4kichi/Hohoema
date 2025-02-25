#nullable enable
using Hohoema.Models.PageNavigation;

namespace Hohoema.Services.Navigations;

public sealed class LoginRedirectPayload : PagePayloadBase
	{
		public HohoemaPageType RedirectPageType { get; set; }
		public string RedirectParamter { get; set; }
	}


