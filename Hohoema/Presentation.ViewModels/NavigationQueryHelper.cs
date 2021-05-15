using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels
{
    public static class NavigationQueryHelper
    {
        public static string ToQueryString(this IEnumerable<KeyValuePair<string, string>> query)
        {
            return String.Join("&", query.Select(x => x.Key + "=" + Uri.EscapeDataString(x.Value)));
        }
    }
}
