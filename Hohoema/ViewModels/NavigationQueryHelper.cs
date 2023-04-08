using System;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.ViewModels;

public static class NavigationQueryHelper
{
    public static string ToQueryString(this IEnumerable<KeyValuePair<string, string>> query)
    {
        return String.Join("&", query.Select(x => x.Key + "=" + Uri.EscapeDataString(x.Value)));
    }

    public static string ToQueryStringWithoutEscape(this IEnumerable<KeyValuePair<string, string>> query)
    {
        return String.Join("&", query.Select(x => x.Key + "=" + x.Value));
    }
}
