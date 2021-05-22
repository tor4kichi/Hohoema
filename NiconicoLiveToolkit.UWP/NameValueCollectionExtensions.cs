using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace NiconicoToolkit
{
    internal static class NameValueCollectionExtensions
    {
        public static StringBuilder ToQueryString(this NameValueCollection nvc, StringBuilder sb = null)
        {
            sb ??= new StringBuilder();

            bool isFirst = true;
            foreach (string key in nvc.Keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;

                string[] values = nvc.GetValues(key);
                if (values == null) continue;

                foreach (string value in values)
                {
                    sb.Append(isFirst ? "?" : "&");
                    sb.Append(Uri.EscapeDataString(key));
                    sb.Append("=");
                    sb.Append(Uri.EscapeDataString(value));
                }

                isFirst = false;
            }

            return sb;
        }
    }

    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendQueryString(this StringBuilder sb, NameValueCollection nvc)
        {
            return nvc.ToQueryString(sb);
        }
    }
}
